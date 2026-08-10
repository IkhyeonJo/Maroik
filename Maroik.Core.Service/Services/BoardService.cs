using Maroik.Core.Domain.Board;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IBoardService"/> for forum post management.
/// File attachments are virus-scanned by ClamAV, image-validated,
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
    private readonly IAttachmentContentService _attachmentContent;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<BoardService> _logger;

    /// <summary>
    /// Maps each board type name to its corresponding file-storage area folder name.
    /// Used when building attachment storage paths.
    /// </summary>
    private static readonly Dictionary<string, string> _boardAreaMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(BoardType.FreeForum)] = "Forum",
        [nameof(BoardType.PrivateNote)] = "Management"
    };

    /// <summary>Initializes a new instance of <see cref="BoardService"/> with the supplied dependencies.</summary>
    public BoardService(
        IBoardRepository boardRepository,
        IBoardAttachedFileRepository boardAttachedFileRepository,
        IBoardCommentRepository boardCommentRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IAttachmentContentService attachmentContent,
        IOptions<ServerSetting> settings,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _attachmentContent = attachmentContent;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type, CancellationToken ct = default)
        => (await _boardRepository.GetByTypeAsync(type, ct)).Select(BoardMapper.ToResponse);

    /// <inheritdoc />
    public async Task<BoardResponse?> GetBoardByIdAsync(long id, CancellationToken ct = default)
    {
        Board? board = await _boardRepository.FindActiveByIdAsync(id, ct);
        return board == null ? null : BoardMapper.ToResponse(board);
    }

    /// <inheritdoc />
    public async Task<BoardAttachedFileDto?> GetAttachedFileByBoardIdAsync(long boardId, CancellationToken ct = default)
    {
        BoardAttachedFile? file = await _boardAttachedFileRepository.FindByBoardIdAsync(boardId, ct);
        return file == null ? null : BoardAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFileDto?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds, CancellationToken ct = default)
    {
        Dictionary<long, BoardAttachedFile?> domain = await _boardAttachedFileRepository.GetByBoardIdsAsync(boardIds, ct);
        return domain.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value == null ? null : (BoardAttachedFileDto?)BoardAttachedFileMapper.ToResponse(kvp.Value));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId, CancellationToken ct = default)
        => (await _boardCommentRepository.GetByBoardIdOrderedAsync(boardId, ct)).Select(BoardCommentMapper.ToResponse);

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds, CancellationToken ct = default)
        => await _boardCommentRepository.GetCommentCountsByBoardIdsAsync(boardIds, ct);

    /// <inheritdoc />
    public async Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileDto? attachedFile, CancellationToken ct = default)
    {
        var fileError = _attachmentContent.ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string content = _attachmentContent.SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            var boardResult = Board.Create(request.Type, request.Title, content, request.Writer);
            if (boardResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(boardResult.FirstError.Description);
            }

            var board = boardResult.Value;
            if (request.Locked) board.Lock();
            if (request.Noticed) board.Pin();

            long boardId = await _boardRepository.WriteBoardAsync(board, ct);

            if (attachedFile != null)
            {
                // Build a unique storage path and upload the file, then persist the attachment record.
                string filePath = BuildAttachedFilePath(request.Type!, boardId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
                await _boardAttachedFileRepository.CreateAsync(
                    BoardAttachedFile.Reconstitute(0, boardId, attachedFile.Size,
                        Path.GetFileNameWithoutExtension(attachedFile.FileName),
                        Path.GetExtension(attachedFile.FileName),
                        filePath.Replace('\\', '/')), ct); // Normalize path separators for URL use.
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write board post (Type={Type})", request.Type);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileDto? newFile, CancellationToken ct = default)
    {
        var fileError = _attachmentContent.ValidateAttachedFile(newFile);
        if (fileError != null) return fileError;

        string content = _attachmentContent.SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            Board? board = await _boardRepository.FindActiveByIdAsync(request.Id, ct);

            if (board == null || board.Deleted)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            // Only the original writer may edit their own post.
            if (!board.CanBeEditedBy(writerNickname))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            var updateResult = board.Update(request.Title, content);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            // Apply lock and pin toggles from the request.
            if (request.Locked) board.Lock(); else board.Unlock();
            if (request.Noticed) board.Pin(); else board.Unpin();

            await _boardRepository.UpdateEntityAsync(board, ct);

            BoardAttachedFile? previous = await _boardAttachedFileRepository.FindByBoardIdAsync(request.Id, ct);
            await HandleAttachmentAsync(board.Type, board.Id, previous, newFile, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to edit board post {BoardId}", request.Id);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin, CancellationToken ct = default)
    {
        try
        {
            Board? board = await _boardRepository.FindActiveByIdAsync(boardId, ct);

            if (board == null || board.Deleted)
                return ServiceResult.Fail("Input is invalid");

            // Admins may delete any post; non-admins may only delete their own.
            if (!board.CanBeDeletedBy(requesterNickname, isAdmin))
                return ServiceResult.Fail("You do not have permission to delete.");

            var deleteResult = board.SoftDelete();
            if (deleteResult.IsError)
                return ServiceResult.Fail(deleteResult.FirstError.Description);

            await _boardRepository.UpdateEntityAsync(board, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete board post {BoardId}", boardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request, string? requiredOwnerNickname = null, CancellationToken ct = default)
    {
        string content = _attachmentContent.SanitizeContent(request.Content ?? "");

        try
        {
            Board? board = await _boardRepository.FindActiveByIdAsync(request.BoardId, ct);
            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            // When requiredOwnerNickname is set, restrict commenting to the post owner only.
            if (requiredOwnerNickname != null && !board.IsOwnedBy(requiredOwnerNickname))
                return ServiceResult.Fail("You do not have permission to write a comment.");

            IEnumerable<BoardComment> existing = await _boardCommentRepository.GetByBoardIdOrderedAsync(request.BoardId, ct);

            // The comment order is the current count, giving sequential ordering starting at 0.
            long order = existing.Count();

            var commentResult = BoardComment.Create(request.BoardId, order, request.AvatarImagePath, request.Writer, content);
            if (commentResult.IsError)
                return ServiceResult.Fail(commentResult.FirstError.Description);

            var addResult = board.AddComment(commentResult.Value);
            if (addResult.IsError)
                return ServiceResult.Fail(addResult.FirstError.Description);

            await _boardCommentRepository.CreateAsync(commentResult.Value, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to write comment on board {BoardId}", request.BoardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> IncrementBoardViewAsync(long boardId, CancellationToken ct = default)
    {
        try
        {
            await _boardRepository.IncrementViewAsync(boardId, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to increment view count for board {BoardId}", boardId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin, CancellationToken ct = default)
    {
        try
        {
            BoardComment? comment = await _boardCommentRepository.FindByIdAsync(commentId, ct);
            if (comment == null)
                return ServiceResult.Fail("Input is invalid");

            // Admins may delete any comment; non-admins may only delete their own.
            if (!comment.CanBeDeletedBy(requesterNickname, isAdmin))
                return ServiceResult.Fail("You do not have permission to delete.");

            comment.SoftDelete();
            await _boardCommentRepository.UpdateEntityAsync(comment, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete comment {CommentId}", commentId);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Handles the four possible attachment states when editing a board post:
    /// replace (previous + new), clear (previous only), add (new only), or no-op (neither).
    /// </summary>
    private async Task HandleAttachmentAsync(string boardType, long boardId, BoardAttachedFile? previous, AttachedFileDto? newFile, CancellationToken ct = default)
    {
        if (previous != null && newFile != null)
        {
            // Replace: upload the new file and overwrite the existing attachment record.
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
            await _boardAttachedFileRepository.UpdateEntityAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')), ct);
        }
        else if (previous != null && newFile == null)
        {
            // Clear: zero-out the attachment record without uploading anything.
            await _boardAttachedFileRepository.UpdateEntityAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, 0, "", "", ""), ct);
        }
        else if (previous == null && newFile != null)
        {
            // Add: upload and create a new attachment record for a post that had none before.
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
            await _boardAttachedFileRepository.CreateAsync(
                BoardAttachedFile.Reconstitute(0, boardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')), ct);
        }
        // previous == null && newFile == null: nothing to do.
    }

    /// <inheritdoc />
    public Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileDto file, string area, string boardType, CancellationToken ct = default)
        => _attachmentContent.UploadSummernoteImageAsync(file, area, boardType, ct);

    /// <inheritdoc />
    public Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken ct = default)
        => _attachmentContent.DownloadFileAsync(filePath, ct);

    /// <summary>
    /// Builds the remote storage path for a board attachment.
    /// A GUID prefix is prepended to the file name to prevent collisions.
    /// </summary>
    private static string BuildAttachedFilePath(string boardType, long boardId, string fileName)
    {
        // Map the board type to the correct storage area; default to "Forum" for unknown types.
        string area = _boardAreaMap.GetValueOrDefault(boardType, "Forum");
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", area, boardType, "boardAttachedFiles", $"{boardId}", $"{guid}{ext}");
    }

    /// <inheritdoc />
    public Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct = default)
        => _attachmentContent.PrepareHtmlForDisplayAsync(html, ct);
}
