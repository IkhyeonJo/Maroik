using ImageMagick;
using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IBoardService"/> for forum post management.
/// File attachments are virus-scanned by ClamAV, image-processed by ImageMagick,
/// and stored/retrieved via the file-storage microservice (<see cref="IFileClient"/>).
/// Summernote inline images are uploaded to the file-storage service and the returned URL
/// is embedded in the HTML content. Post/comment write operations use <see cref="IUnitOfWork"/>
/// transactions to keep post and attachment records in sync.
/// </summary>
public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
    private readonly IBoardCommentRepository _boardCommentRepository;
    private readonly IFileClient _fileClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<BoardService> _logger;

    private static readonly Dictionary<string, string> BoardAreaMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["FreeForum"]   = "Forum",
        ["PrivateNote"] = "Management"
    };

    /// <summary>Initializes a new instance of <see cref="BoardService"/> with the supplied dependencies.</summary>
    public BoardService(
        IBoardRepository boardRepository,
        IBoardAttachedFileRepository boardAttachedFileRepository,
        IBoardCommentRepository boardCommentRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IOptions<ServerSetting> settings,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type)
        => (await _boardRepository.GetOneTypeBoardsAsync(type)).Select(BoardMapper.ToResponse);

    /// <inheritdoc />
    public async Task<BoardResponse?> GetBoardByIdAsync(long id)
    {
        Board? board = await _boardRepository.GetBoardByIdAsync(id);
        return board == null ? null : BoardMapper.ToResponse(board);
    }

    /// <inheritdoc />
    public async Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId)
    {
        BoardAttachedFile? file = await _boardAttachedFileRepository.GetByBoardIdAsync(boardId);
        return file == null ? null : BoardAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFileResponse?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds)
    {
        Dictionary<long, BoardAttachedFile?> domain = await _boardAttachedFileRepository.GetByBoardIdsAsync(boardIds);
        return domain.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value == null ? null : (BoardAttachedFileResponse?)BoardAttachedFileMapper.ToResponse(kvp.Value));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId)
        => (await _boardCommentRepository.GetOneBoardCommentsAsync(boardId)).Select(BoardCommentMapper.ToResponse);

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds)
        => await _boardCommentRepository.GetCommentCountsByBoardIdsAsync(boardIds);

    private static ServiceResult? ValidateAttachedFile(AttachedFileInfo? attachedFile)
    {
        if (attachedFile == null) return null;
        if (Path.GetExtension(attachedFile.FileName) != ".zip")
            return ServiceResult.Fail("Only zip extension allowed.");
        if (attachedFile.Size is not (> 0 and <= 10485760))
            return ServiceResult.Fail("Uploaded file size must be smaller than 10MB.");
        return null;
    }

    /// <inheritdoc />
    public async Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        await _unitOfWork.BeginAsync();
        try
        {
            var boardResult = Board.Create(0, request.Type, request.Title, request.Content, request.Writer);
            if (boardResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(boardResult.FirstError.Description);
            }

            var board = boardResult.Value;
            long boardId = await _boardRepository.WriteBoardAsync(board);

            if (attachedFile != null)
            {
                string filePath = BuildAttachedFilePath(request.Type!, boardId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(
                    BoardAttachedFile.Reconstitute(0, boardId, attachedFile.Size,
                        Path.GetFileNameWithoutExtension(attachedFile.FileName),
                        Path.GetExtension(attachedFile.FileName),
                        filePath.Replace('\\', '/')));
            }

            await _unitOfWork.CommitAsync();
            await _dispatcher.DispatchAsync(board.PopDomainEvents());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write board post (Type={Type})", request.Type);
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile)
    {
        var fileError = ValidateAttachedFile(newFile);
        if (fileError != null) return fileError;

        await _unitOfWork.BeginAsync();
        try
        {
            Board? board = await _boardRepository.GetBoardByIdAsync(request.Id);

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

            var updateResult = board.Update(request.Title, request.Content);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            if (request.Locked) board.Lock(); else board.Unlock();
            if (request.Noticed) board.Pin(); else board.Unpin();

            await _boardRepository.UpdateBoardAsync(board);

            BoardAttachedFile? previous = await _boardAttachedFileRepository.GetByBoardIdAsync(request.Id);
            await HandleAttachmentAsync(board.Type, board.Id, previous, newFile);

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

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin)
    {
        try
        {
            Board? board = await _boardRepository.GetBoardByIdAsync(boardId);

            if (board == null || board.Deleted)
                return ServiceResult.Fail("Input is invalid");

            if (board.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            var deleteResult = board.SoftDelete();
            if (deleteResult.IsError)
                return ServiceResult.Fail(deleteResult.FirstError.Description);

            await _boardRepository.DeleteBoardAsync(board);
            await _dispatcher.DispatchAsync(board.PopDomainEvents());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete board post {BoardId}", boardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request, string? requiredOwnerNickname = null)
    {
        try
        {
            Board? board = await _boardRepository.GetBoardByIdAsync(request.BoardId);
            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            if (requiredOwnerNickname != null && board.Writer != requiredOwnerNickname)
                return ServiceResult.Fail("You do not have permission to write a comment.");

            IEnumerable<BoardComment> existing = await _boardCommentRepository.GetOneBoardCommentsAsync(request.BoardId);
            long order = (long)(existing?.Count() ?? 0);

            var commentResult = BoardComment.Create(0, request.BoardId, order, request.AvatarImagePath, request.Writer, request.Content);
            if (commentResult.IsError)
                return ServiceResult.Fail(commentResult.FirstError.Description);

            var addResult = board.AddComment(commentResult.Value);
            if (addResult.IsError)
                return ServiceResult.Fail(addResult.FirstError.Description);

            await _boardCommentRepository.WriteBoardCommentAsync(commentResult.Value);
            await _dispatcher.DispatchAsync(board.PopDomainEvents());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write comment on board {BoardId}", request.BoardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin)
    {
        try
        {
            BoardComment? comment = await _boardCommentRepository.GetBoardCommentAsync((int)commentId);
            if (comment == null)
                return ServiceResult.Fail("Input is invalid");

            if (comment.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            comment.SoftDelete();
            await _boardCommentRepository.DeleteBoardCommentAsync(comment);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete comment {CommentId}", commentId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    private async Task HandleAttachmentAsync(string boardType, long boardId, BoardAttachedFile? previous, AttachedFileInfo? newFile)
    {
        if (previous != null && newFile != null)
        {
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')));
        }
        else if (previous != null && newFile == null)
        {
            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, 0, "", "", ""));
        }
        else if (previous == null && newFile != null)
        {
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(
                BoardAttachedFile.Reconstitute(0, boardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')));
        }
        // previous == null && newFile == null → nothing to do
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
