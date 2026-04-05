using Maroik.Core.Client.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

public class BoardService : IBoardService
{
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
    private readonly IBoardCommentRepository _boardCommentRepository;
    private readonly IFileClient _fileClient;
    private readonly IUnitOfWork _unitOfWork;

    // Maps board type description to storage area folder name
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
        IUnitOfWork unitOfWork)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
    }

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
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl ?? string.Empty);
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
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            IEnumerable<BoardResponse> boards = await _boardRepository.GetOneTypeBoardsAsync(request.Type ?? "");
            BoardResponse? board = boards.FirstOrDefault(b => b.Id == request.Id && !b.Deleted);

            if (board == null)
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

            // Handle attachment
            List<BoardAttachedFileResponse> allAttached = await _boardAttachedFileRepository.GetAllAsync();
            BoardAttachedFileResponse? previous = allAttached.FirstOrDefault(f => f.BoardId == request.Id);

            if (previous != null && newFile != null)
            {
                string filePath = BuildAttachedFilePath(request.Type!, request.Id, newFile.FileName);
                await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl ?? string.Empty);
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
                string filePath = BuildAttachedFilePath(request.Type!, request.Id, newFile.FileName);
                await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl ?? string.Empty);
                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFileRequest
                {
                    BoardId = request.Id,
                    Size = Convert.ToInt32(newFile.Size),
                    Name = Path.GetFileNameWithoutExtension(newFile.FileName),
                    Extension = Path.GetExtension(newFile.FileName),
                    Path = filePath.Replace('\\', '/')
                });
            }
            // previous == null && newFile == null → nothing to do

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin)
    {
        try
        {
            IEnumerable<BoardResponse> boards = await _boardRepository.GetOneTypeBoardsAsync(boardType);
            BoardResponse? board = boards.FirstOrDefault(b => b.Id == boardId && !b.Deleted);

            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            if (board.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            board.Deleted = true;
            await _boardRepository.DeleteBoardAsync(board.Adapt<BoardRequest>());
            return ServiceResult.Ok();
        }
        catch
        {
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request)
    {
        try
        {
            IEnumerable<BoardCommentResponse> existing = await _boardCommentRepository.GetOneBoardCommentsAsync(request.BoardId);
            request.Order = existing?.Count() ?? 0;
            request.Created = DateTime.UtcNow;
            request.Deleted = false;
            await _boardCommentRepository.WriteBoardCommentAsync(request);
            return ServiceResult.Ok();
        }
        catch
        {
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
        catch
        {
            return ServiceResult.Fail("Input is invalid");
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
