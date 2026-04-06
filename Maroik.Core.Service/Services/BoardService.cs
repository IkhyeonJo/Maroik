using ImageMagick;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
    private readonly IBoardCommentRepository _boardCommentRepository;
    private readonly IFileClient _fileClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<BoardService> _logger;

    private static readonly Dictionary<string, string> BoardAreaMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FreeForum"]   = "Forum",
        ["PrivateNote"] = "Management"
    };

    public BoardService(
        IBoardRepository boardRepository,
        IBoardAttachedFileRepository boardAttachedFileRepository,
        IBoardCommentRepository boardCommentRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IOptions<ServerSetting> settings,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _settings = settings;
        _logger = logger;
    }

    public async Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type)
        => await _boardRepository.GetOneTypeBoardsAsync(type);

    public async Task<BoardResponse?> GetBoardByIdAsync(long id)
        => await _boardRepository.GetBoardByIdAsync(id);

    public async Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId)
        => await _boardAttachedFileRepository.GetByBoardIdAsync(boardId);

    public async Task<Dictionary<long, BoardAttachedFileResponse?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds)
        => await _boardAttachedFileRepository.GetByBoardIdsAsync(boardIds);

    public async Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId)
        => await _boardCommentRepository.GetOneBoardCommentsAsync(boardId);

    public async Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds)
        => await _boardCommentRepository.GetCommentCountsByBoardIdsAsync(boardIds);

    public async Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            request.Created = DateTime.UtcNow;
            request.Updated = DateTime.UtcNow;
            request.View = 0;
            request.Deleted = false;

            await _boardRepository.WriteBoardAsync(request);

            if (attachedFile != null)
            {
                string filePath = BuildAttachedFilePath(request.Type!, request.Id, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFileRequest
                {
                    BoardId = request.Id,
                    Size = Convert.ToInt32(attachedFile.Size),
                    Name = Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Extension = Path.GetExtension(attachedFile.FileName),
                    Path = filePath.Replace('\\', '/')
                });
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write board post (Type={Type})", request.Type);
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            BoardResponse? board = await _boardRepository.GetBoardByIdAsync(request.Id);

            if (board == null || board.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            if (board.Writer != writerNickname)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            board.Title = request.Title;
            board.Content = request.Content;
            board.Locked = request.Locked;
            board.Noticed = request.Noticed;
            board.Updated = DateTime.UtcNow;

            await _boardRepository.UpdateBoardAsync(board.Adapt<BoardRequest>());

            BoardAttachedFileResponse? previous = await _boardAttachedFileRepository.GetByBoardIdAsync(request.Id);

            await HandleAttachmentAsync(request.Type!, request.Id, previous, newFile);

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to edit board post {BoardId}", request.Id);
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin)
    {
        try
        {
            BoardResponse? board = await _boardRepository.GetBoardByIdAsync(boardId);

            if (board == null || board.Deleted)
                return ServiceResult.Fail("Input is invalid");

            if (board.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            board.Deleted = true;
            await _boardRepository.DeleteBoardAsync(board.Adapt<BoardRequest>());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete board post {BoardId}", boardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request, string? requiredOwnerNickname = null)
    {
        try
        {
            BoardResponse? board = await _boardRepository.GetBoardByIdAsync(request.BoardId);
            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            if (requiredOwnerNickname != null && board.Writer != requiredOwnerNickname)
                return ServiceResult.Fail("You do not have permission to write a comment.");

            IEnumerable<BoardCommentResponse> existing = await _boardCommentRepository.GetOneBoardCommentsAsync(request.BoardId);
            request.Order = existing?.Count() ?? 0;
            request.Created = DateTime.UtcNow;
            request.Deleted = false;
            await _boardCommentRepository.WriteBoardCommentAsync(request);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write comment on board {BoardId}", request.BoardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> IncrementBoardViewAsync(long boardId)
    {
        try
        {
            await _boardRepository.IncrementViewAsync(boardId);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to increment view count for board {BoardId}", boardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin)
    {
        try
        {
            BoardCommentResponse? comment = await _boardCommentRepository.GetBoardCommentAsync((int)commentId);
            if (comment == null)
                return ServiceResult.Fail("Input is invalid");

            if (comment.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            comment.Deleted = true;
            await _boardCommentRepository.DeleteBoardCommentAsync(comment.Adapt<BoardCommentRequest>());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete comment {CommentId}", commentId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    private async Task HandleAttachmentAsync(string boardType, long boardId, BoardAttachedFileResponse? previous, AttachedFileInfo? newFile)
    {
        if (previous != null && newFile != null)
        {
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            previous.Size = Convert.ToInt32(newFile.Size);
            previous.Name = Path.GetFileNameWithoutExtension(newFile.FileName);
            previous.Extension = Path.GetExtension(newFile.FileName);
            previous.Path = filePath.Replace('\\', '/');
            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previous.Adapt<BoardAttachedFileRequest>());
        }
        else if (previous != null && newFile == null)
        {
            previous.Size = 0;
            previous.Name = "";
            previous.Extension = "";
            previous.Path = "";
            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previous.Adapt<BoardAttachedFileRequest>());
        }
        else if (previous == null && newFile != null)
        {
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFileRequest
            {
                BoardId = boardId,
                Size = Convert.ToInt32(newFile.Size),
                Name = Path.GetFileNameWithoutExtension(newFile.FileName),
                Extension = Path.GetExtension(newFile.FileName),
                Path = filePath.Replace('\\', '/')
            });
        }
        // previous == null && newFile == null → nothing to do
    }

    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string area, string boardType)
    {
        try
        {
            using var image = new MagickImage(new MemoryStream(file.Bytes));
        }
        catch (MagickException)
        {
            return SummernoteUploadResult.Fail("Invalid image file.");
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine("upload", area, boardType, "summernote", "images", imageFile);

        bool uploaded = await _fileClient.UploadAsync(file.Bytes, file.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        if (!uploaded)
            return SummernoteUploadResult.Fail("Input is invalid");

        byte[] fileBytes = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        return SummernoteUploadResult.Ok(fileBytes, file.ContentType, imageFile, filePath);
    }

    public async Task<byte[]?> DownloadFileAsync(string filePath)
    {
        try
        {
            return await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download file {FilePath}", filePath);
            return null;
        }
    }

    private static string BuildAttachedFilePath(string boardType, long boardId, string fileName)
    {
        string area = BoardAreaMap.TryGetValue(boardType, out string? a) ? a : "Forum";
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", area, boardType, "boardAttachedFiles", $"{boardId}", $"{guid}{ext}");
    }
}
