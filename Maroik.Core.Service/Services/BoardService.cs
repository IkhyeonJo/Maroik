using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Filters;
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
    private readonly IRsaService _rsa;
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
        IOptions<ServerSetting> settings,
        ILogger<BoardService> logger)
    {
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _rsa = rsa;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type)
        => (await _boardRepository.FindAsync([FilterCondition.Equal("Type", type)])).Select(BoardMapper.ToResponse);

    /// <inheritdoc />
    public async Task<BoardResponse?> GetBoardByIdAsync(long id)
    {
        Board? board = (await _boardRepository.FindAsync([
            FilterCondition.Equal("Id", id),
            FilterCondition.Equal("Deleted", false)
        ])).FirstOrDefault();
        return board == null ? null : BoardMapper.ToResponse(board);
    }

    /// <inheritdoc />
    public async Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId)
    {
        BoardAttachedFile? file = (await _boardAttachedFileRepository.FindAsync([FilterCondition.Equal("BoardId", boardId)])).FirstOrDefault();
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
        => (await _boardCommentRepository.FindAsync([FilterCondition.Equal("BoardId", boardId)], orderByProperty: "Order")).Select(BoardCommentMapper.ToResponse);

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds)
        => await _boardCommentRepository.GetCommentCountsByBoardIdsAsync(boardIds);

    /// <summary>
    /// Validates an attached file: only <c>.zip</c> files up to 10 MB are accepted.
    /// Returns <see langword="null"/> when the file passes validation (or when no file is provided).
    /// </summary>
    /// <param name="attachedFile">The file to validate, or <see langword="null"/> when no file was uploaded.</param>
    /// <returns>A failure <see cref="ServiceResult"/> describing the violation, or <see langword="null"/> on success.</returns>
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

        string content = SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync();
        try
        {
            var boardResult = Board.Create(request.Type, request.Title, content, request.Writer);
            if (boardResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(boardResult.FirstError.Description);
            }

            var board = boardResult.Value;
            long boardId = await _boardRepository.WriteBoardAsync(board);

            if (attachedFile != null)
            {
                // Build a unique storage path and upload the file, then persist the attachment record.
                string filePath = BuildAttachedFilePath(request.Type!, boardId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                await _boardAttachedFileRepository.CreateAsync(
                    BoardAttachedFile.Reconstitute(0, boardId, attachedFile.Size,
                        Path.GetFileNameWithoutExtension(attachedFile.FileName),
                        Path.GetExtension(attachedFile.FileName),
                        filePath.Replace('\\', '/'))); // Normalise path separators for URL use.
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

    /// <inheritdoc />
    public async Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile)
    {
        var fileError = ValidateAttachedFile(newFile);
        if (fileError != null) return fileError;

        string content = SanitizeAndDecryptContent(request.Content ?? "");

        await _unitOfWork.BeginAsync();
        try
        {
            Board? board = (await _boardRepository.FindAsync([FilterCondition.Equal("Id", request.Id), FilterCondition.Equal("Deleted", false)])).FirstOrDefault();

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

            await _boardRepository.UpdateEntityAsync(board);

            BoardAttachedFile? previous = (await _boardAttachedFileRepository.FindAsync([FilterCondition.Equal("BoardId", request.Id)])).FirstOrDefault();
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
            Board? board = (await _boardRepository.FindAsync([FilterCondition.Equal("Id", boardId), FilterCondition.Equal("Deleted", false)])).FirstOrDefault();

            if (board == null || board.Deleted)
                return ServiceResult.Fail("Input is invalid");

            // Admins may delete any post; non-admins may only delete their own.
            if (board.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            var deleteResult = board.SoftDelete();
            if (deleteResult.IsError)
                return ServiceResult.Fail(deleteResult.FirstError.Description);

            await _boardRepository.UpdateEntityAsync(board);
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
        string content = SanitizeContent(request.Content ?? "");

        try
        {
            Board? board = (await _boardRepository.FindAsync([FilterCondition.Equal("Id", request.BoardId), FilterCondition.Equal("Deleted", false)])).FirstOrDefault();
            if (board == null)
                return ServiceResult.Fail("Input is invalid");

            // When requiredOwnerNickname is set, restrict commenting to the post owner only.
            if (requiredOwnerNickname != null && board.Writer != requiredOwnerNickname)
                return ServiceResult.Fail("You do not have permission to write a comment.");

            IEnumerable<BoardComment> existing = await _boardCommentRepository.FindAsync([FilterCondition.Equal("BoardId", request.BoardId)], orderByProperty: "Order");

            // The comment order is the current count, giving sequential ordering starting at 0.
            long order = (long)(existing?.Count() ?? 0);

            var commentResult = BoardComment.Create(request.BoardId, order, request.AvatarImagePath, request.Writer, content);
            if (commentResult.IsError)
                return ServiceResult.Fail(commentResult.FirstError.Description);

            var addResult = board.AddComment(commentResult.Value);
            if (addResult.IsError)
                return ServiceResult.Fail(addResult.FirstError.Description);

            await _boardCommentRepository.CreateAsync(commentResult.Value);
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
            BoardComment? comment = (await _boardCommentRepository.FindAsync([FilterCondition.Equal("Id", (int)commentId)])).FirstOrDefault();
            if (comment == null)
                return ServiceResult.Fail("Input is invalid");

            // Admins may delete any comment; non-admins may only delete their own.
            if (comment.Writer != requesterNickname && !isAdmin)
                return ServiceResult.Fail("You do not have permission to delete.");

            comment.SoftDelete();
            await _boardCommentRepository.UpdateEntityAsync(comment);
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
    /// <param name="boardType">Board type, used to build the storage path.</param>
    /// <param name="boardId">ID of the board post the attachment belongs to.</param>
    /// <param name="previous">The existing attachment record, or <see langword="null"/> if none exists.</param>
    /// <param name="newFile">The incoming file from the edit request, or <see langword="null"/> if not provided.</param>
    private async Task HandleAttachmentAsync(string boardType, long boardId, BoardAttachedFile? previous, AttachedFileInfo? newFile)
    {
        if (previous != null && newFile != null)
        {
            // Replace: upload the new file and overwrite the existing attachment record.
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            await _boardAttachedFileRepository.UpdateEntityAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')));
        }
        else if (previous != null && newFile == null)
        {
            // Clear: zero-out the attachment record without uploading anything.
            await _boardAttachedFileRepository.UpdateEntityAsync(
                BoardAttachedFile.Reconstitute(previous.Id, previous.BoardId, 0, "", "", ""));
        }
        else if (previous == null && newFile != null)
        {
            // Add: upload and create a new attachment record for a post that had none before.
            string filePath = BuildAttachedFilePath(boardType, boardId, newFile.FileName);
            await _fileClient.UploadAsync(newFile.Bytes, newFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            await _boardAttachedFileRepository.CreateAsync(
                BoardAttachedFile.Reconstitute(0, boardId, newFile.Size,
                    Path.GetFileNameWithoutExtension(newFile.FileName),
                    Path.GetExtension(newFile.FileName),
                    filePath.Replace('\\', '/')));
        }
        // previous == null && newFile == null: nothing to do.
    }

    /// <inheritdoc />
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string area, string boardType)
    {
        // Validate the bytes as a real image by parsing them with ImageMagick before storing.
        try
        {
            using var image = new MagickImage(new MemoryStream(file.Bytes));
        }
        catch (MagickException)
        {
            return SummernoteUploadResult.Fail("Invalid image file.");
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        // Use a GUID-based file name to avoid collisions and prevent path traversal via the original name.
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

    /// <summary>
    /// Builds the remote storage path for a board attachment.
    /// A GUID prefix is prepended to the file name to prevent collisions.
    /// </summary>
    /// <param name="boardType">The board type string (e.g., <c>"FreeForum"</c>).</param>
    /// <param name="boardId">The board post ID used as a directory segment.</param>
    /// <param name="fileName">The original file name (extension is preserved).</param>
    /// <returns>A platform-native path string suitable for passing to <see cref="IFileClient"/>.</returns>
    private static string BuildAttachedFilePath(string boardType, long boardId, string fileName)
    {
        // Map the board type to the correct storage area; default to "Forum" for unknown types.
        string area = BoardAreaMap.TryGetValue(boardType, out string? a) ? a : "Forum";
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", area, boardType, "boardAttachedFiles", $"{boardId}", $"{guid}{ext}");
    }

    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        bool hasImages = false;

        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img") ?? [])
        {
            hasImages = true;
            string filePath = imgTag.GetAttributeValue("alt", "");
            byte[]? fileData = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            if (fileData != null)
            {
                _ = imgTag.SetAttributeValue("data-file", Convert.ToBase64String(fileData));
                _ = imgTag.SetAttributeValue("data-contenttype", GetImageContentType(filePath));
                _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
            }
        }

        return (doc.DocumentNode.OuterHtml, hasImages);
    }

    /// <summary>Sanitizes HTML to remove unsafe markup and decrypts RSA-encrypted file paths in img alt attributes.</summary>
    private string SanitizeAndDecryptContent(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        string sanitized = sanitizer.Sanitize(html);

        var doc = new HtmlDocument();
        doc.LoadHtml(sanitized);
        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img") ?? [])
        {
            string encryptedPath = imgTag.GetAttributeValue("alt", "");
            _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedPath));
        }
        return doc.DocumentNode.OuterHtml;
    }

    /// <summary>Sanitizes HTML to remove unsafe markup (used for comment content, which contains no inline images).</summary>
    private static string SanitizeContent(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        return sanitizer.Sanitize(html);
    }

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
