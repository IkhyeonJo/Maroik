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
    private readonly IRsaService _rsa;
    private readonly IImageValidator _imageValidator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;
    private readonly IHtmlParser _htmlParser;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<BoardService> _logger;

    /// <summary>
    /// Maps each board type name to its corresponding file-storage area folder name.
    /// Used when building attachment storage paths.
    /// </summary>
    private static readonly Dictionary<string, string> BoardAreaMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(BoardType.FreeForum)]   = "Forum",
        [nameof(BoardType.PrivateNote)] = "Management"
    };

    /// <summary>Initializes a new instance of <see cref="BoardService"/> with the supplied dependencies.</summary>
    public BoardService(
        IBoardRepository boardRepository,
        IBoardAttachedFileRepository boardAttachedFileRepository,
        IBoardCommentRepository boardCommentRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IRsaService rsa,
        IImageValidator imageValidator,
        IHtmlContentSanitizer htmlSanitizer,
        IHtmlParser htmlParser,
        IOptions<ServerSetting> settings,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _rsa = rsa;
        _imageValidator = imageValidator;
        _htmlSanitizer = htmlSanitizer;
        _htmlParser = htmlParser;
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
    public async Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId, CancellationToken ct = default)
    {
        BoardAttachedFile? file = await _boardAttachedFileRepository.FindByBoardIdAsync(boardId, ct);
        return file == null ? null : BoardAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFileResponse?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds, CancellationToken ct = default)
    {
        Dictionary<long, BoardAttachedFile?> domain = await _boardAttachedFileRepository.GetByBoardIdsAsync(boardIds, ct);
        return domain.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value == null ? null : (BoardAttachedFileResponse?)BoardAttachedFileMapper.ToResponse(kvp.Value));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId, CancellationToken ct = default)
        => (await _boardCommentRepository.GetByBoardIdOrderedAsync(boardId, ct)).Select(BoardCommentMapper.ToResponse);

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds, CancellationToken ct = default)
        => await _boardCommentRepository.GetCommentCountsByBoardIdsAsync(boardIds, ct);

    /// <summary>
    /// Validates an attached file: only <c>.zip</c> files up to 10 MB are accepted.
    /// Returns <see langword="null"/> when the file passes validation (or when no file is provided).
    /// </summary>
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
    public async Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile, CancellationToken ct = default)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string content = SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            var boardResult = Board.Create(request.Type, request.Title, content, request.Writer);
            if (boardResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(boardResult.FirstError.Description);
            }

            var board = boardResult.Value;
            long boardId = await _boardRepository.WriteBoardAsync(board, ct);

            if (attachedFile != null)
            {
                // Build a unique storage path and upload the file, then persist the attachment record.
                string filePath = BuildAttachedFilePath(request.Type!, boardId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
                await _boardAttachedFileRepository.CreateAsync(
                    BoardAttachedFile.Reconstitute(0, boardId, attachedFile.Size,
                        Path.GetFileNameWithoutExtension(attachedFile.FileName),
                        Path.GetExtension(attachedFile.FileName),
                        filePath.Replace('\\', '/')), ct); // Normalise path separators for URL use.
            }

            await _unitOfWork.CommitAsync(ct);
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
    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile, CancellationToken ct = default)
    {
        var fileError = ValidateAttachedFile(newFile);
        if (fileError != null) return fileError;

        string content = SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            Board? board = await _boardRepository.FindActiveByIdAsync(request.Id, ct);

            if (board == null || board.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            // Only the original writer may edit their own post.
            if (board.Writer != writerNickname)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            var updateResult = board.Update(request.Title, content);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
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
            await _unitOfWork.RollbackAsync();
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
            if (board.Writer != requesterNickname && !isAdmin)
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
        string content = SanitizeContent(request.Content ?? "");

        try
        {
            Board? board = await _boardRepository.FindActiveByIdAsync(request.BoardId, ct);
            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            // When requiredOwnerNickname is set, restrict commenting to the post owner only.
            if (requiredOwnerNickname != null && board.Writer != requiredOwnerNickname)
                return ServiceResult.Fail("You do not have permission to write a comment.");

            IEnumerable<BoardComment> existing = await _boardCommentRepository.GetByBoardIdOrderedAsync(request.BoardId, ct);

            // The comment order is the current count, giving sequential ordering starting at 0.
            long order = (long)(existing?.Count() ?? 0);

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
            if (comment.Writer != requesterNickname && !isAdmin)
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
    private async Task HandleAttachmentAsync(string boardType, long boardId, BoardAttachedFile? previous, AttachedFileInfo? newFile, CancellationToken ct = default)
    {
        if (previous != null && newFile != null)
        {
            // Replace: upload the new file and overwrite the existing attachment record.
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
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
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
            await _boardAttachedFileRepository.CreateAsync(
                BoardAttachedFile.Reconstitute(0, boardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')), ct);
        }
        // previous == null && newFile == null: nothing to do.
    }

    /// <inheritdoc />
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string area, string boardType, CancellationToken ct = default)
    {
        if (!_imageValidator.IsValidImage(file.Bytes))
            return SummernoteUploadResult.Fail("Invalid image file.");

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        // Use a GUID-based file name to avoid collisions and prevent path traversal via the original name.
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine("upload", area, boardType, "summernote", "images", imageFile);

        bool uploaded = await _fileClient.UploadAsync(file.Bytes, file.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        if (!uploaded)
            return SummernoteUploadResult.Fail("Input is invalid");

        byte[] fileBytes = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        return SummernoteUploadResult.Ok(fileBytes, file.ContentType, imageFile, filePath);
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            return await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download file {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// Builds the remote storage path for a board attachment.
    /// A GUID prefix is prepended to the file name to prevent collisions.
    /// </summary>
    private static string BuildAttachedFilePath(string boardType, long boardId, string fileName)
    {
        // Map the board type to the correct storage area; default to "Forum" for unknown types.
        string area = BoardAreaMap.TryGetValue(boardType, out string? a) ? a : "Forum";
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", area, boardType, "boardAttachedFiles", $"{boardId}", $"{guid}{ext}");
    }

    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct = default)
    {
        return await _htmlParser.TransformImageAttributesAsync(html, async (alt, token) =>
        {
            byte[]? fileData = await _fileClient.DownloadAsync(alt, _settings.Value.FileStorageBaseUrl ?? string.Empty, token);
            if (fileData == null) return null;
            return new HtmlImgPatch(
                NewAlt: _rsa.Encrypt(alt),
                DataFile: Convert.ToBase64String(fileData),
                DataContentType: GetImageContentType(alt));
        }, ct);
    }

    /// <summary>Sanitizes HTML to remove unsafe markup and decrypts RSA-encrypted file paths in img alt attributes.</summary>
    private string SanitizeAndDecryptContent(string html)
    {
        string sanitized = _htmlSanitizer.Sanitize(html);
        return _htmlParser.TransformImageAttributes(sanitized, alt => new HtmlImgPatch(NewAlt: _rsa.Decrypt(alt)));
    }

    /// <summary>Sanitizes HTML to remove unsafe markup (used for comment content, which contains no inline images).</summary>
    private string SanitizeContent(string html) => _htmlSanitizer.Sanitize(html);

    /// <summary>Returns the MIME content type for Summernote-uploaded images by file extension.</summary>
    private static string GetImageContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "application/octet-stream"
        };
}
