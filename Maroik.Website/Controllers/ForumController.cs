using Maroik.Core.Domain.Media;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Website.Attributes;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Maroik.Website.Models;
using Maroik.Website.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Controllers;

/// <summary>
/// Manages the free forum: listing, writing, editing, and deleting posts and comments,
/// plus Summernote inline-image uploads.
/// </summary>
public class ForumController : Controller
{
    private readonly IRsaService _rsa;
    private readonly IHtmlLocalizer<ForumController> _localizer;
    private readonly ILogger<ForumController> _logger;
    private readonly IBoardService _boardService;
    private readonly IAccountService _accountService;
    private readonly ISessionService _sessionService;
    private readonly IOptions<ServerSetting> _serverSettings;
    private static readonly FileExtensionContentTypeProvider _contentTypeProvider = new();

    /// <summary>Initializes a new instance of <see cref="ForumController"/> with the supplied dependencies.</summary>
    public ForumController(
        IHtmlLocalizer<ForumController> localizer,
        ILogger<ForumController> logger,
        IBoardService boardService,
        IAccountService accountService,
        IRsaService rsa,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
    {
        _rsa = rsa;
        _sessionService = sessionService;
        _localizer = localizer;
        _logger = logger;
        _boardService = boardService;
        _accountService = accountService;
        _serverSettings = serverSettings;
    }

    #region FreeForum

    #region Create

    #region Write

    #region FreeBoard
    /// <summary>Submits a new free-forum post (with optional file attachment).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WriteFreeBoard(BoardInputViewModel boardInputViewModel, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            if (loggedInAccount.Role != Role.Admin && loggedInAccount.Role != Role.User)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            BoardRequest boardRequest = new()
            {
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Writer = loggedInAccount.Nickname!,
                Locked = boardInputViewModel.Locked,
                Noticed = loggedInAccount.Role == Role.Admin && boardInputViewModel.Noticed
            };

            AttachedFileDto? attachedFile = await boardInputViewModel.UploadedFile.ToAttachedFileInfoAsync(ct);

            ServiceResult result = await _boardService.WriteBoardAsync(boardRequest, attachedFile, ct);
            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully created."].Value })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region FreeComment
    /// <summary>Submits a comment on a free-forum post.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WriteFreeComment([FromBody] BoardCommentInputViewModel boardCommentInputViewModel, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                return Json(new { result = false, error = _localizer["Please enter a comment."].Value });

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write comment."].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            BoardCommentRequest commentRequest = new()
            {
                BoardId = boardCommentInputViewModel.BoardId,
                AvatarImagePath = loggedInAccount.AvatarImagePath!,
                Writer = loggedInAccount.Nickname!,
                Content = boardCommentInputViewModel.Content
            };

            ServiceResult result = await _boardService.WriteCommentAsync(commentRequest, isAdmin: loggedInAccount.Role == Role.Admin, requiredType: EnumHelper.GetDescription(BoardType.FreeForum), ct: ct);
            return result.Success
                ? Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Summernote Image File Upload
    /// <summary>Uploads a Summernote inline image and returns its URL.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile? summernoteImageFile, CancellationToken ct)
    {
        if (summernoteImageFile == null)
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > _serverSettings.Value.MaxAttachedFileSizeBytes)
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than {0}MB.", _serverSettings.Value.MaxAttachedFileSizeBytes / (1024 * 1024)].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (!ImageUploadPolicy.IsAllowedExtension(ext))
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await summernoteImageFile.CopyToAsync(ms, ct);
            bytes = ms.ToArray();
        }

        var file = new AttachedFileDto
        {
            Bytes = bytes,
            ContentType = summernoteImageFile.ContentType,
            FileName = summernoteImageFile.FileName,
            Size = summernoteImageFile.Length
        };

        SummernoteUploadResult uploadResult = await _boardService.UploadSummernoteImageAsync(file, "Forum", EnumHelper.GetDescription(BoardType.FreeForum), ct);

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = _localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        return Ok(new
        {
            result = true,
            file = File(uploadResult.FileBytes!, uploadResult.ContentType!, uploadResult.FileName!),
            filePath = _rsa.Encrypt(uploadResult.FilePath!)
        });
    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write
    /// <summary>Displays the free-forum list or a single post (list / write / view / edit).</summary>
    public async Task<IActionResult> FreeForum(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "", CancellationToken ct = default)
    {
        switch (method)
        {
            // Write
            case "write":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new() { Method = method };

                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
            }
            // Detail view
            case "detail" when boardId == null:
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            case "detail":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    Method = method,
                    DetailCurrentPage = page,
                    DetailBoardId = (int)boardId
                };

                BoardResponse? freeBoard = await _boardService.GetBoardByIdAsync((long)boardId, EnumHelper.GetDescription(BoardType.FreeForum), ct);
                BoardAttachedFileDto? boardAttachedFile = await _boardService.GetAttachedFileByBoardIdAsync((long)boardId, ct);

                try
                {
                    if (freeBoard == null || freeBoard.Deleted)
                        return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                    AccountResponse? loggedInAccount = _sessionService.GetAccount();
                    if (!_boardService.CanView(freeBoard, loggedInAccount))
                        return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                    await _boardService.IncrementBoardViewAsync((long)boardId, ct);

                    (freeBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(freeBoard.Content ?? "", ct);

                    freeForumOutputViewModel.LoggedInAccount = loggedInAccount ?? new AccountResponse { Role = Role.Anonymous };
                    freeForumOutputViewModel.LoggedInAccountTimeZoneIanaId = freeForumOutputViewModel.LoggedInAccount.TimeZoneIanaId ?? "";

                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.View,
                        Content = freeBoard.Content,
                        Updated = freeBoard.Updated,
                        BoardAttachedFileName = boardAttachedFile?.Name ?? "",
                        BoardAttachedFileExtension = boardAttachedFile?.Extension ?? "",
                        BoardAttachedFileSize = boardAttachedFile?.Size ?? 0,
                        BoardAttachedFilePath = boardAttachedFile?.Path ?? "",
                        IsImgTagIncluded = isImgTagIncluded,
                        CanModify = freeForumOutputViewModel.LoggedInAccount.IsOwner(freeBoard.Writer),
                        CanDelete = freeForumOutputViewModel.LoggedInAccount.IsOwnerOrAdmin(freeBoard.Writer)
                    };

                    if (!string.IsNullOrEmpty(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                    {
                        byte[]? fileData = await _boardService.DownloadFileAsync(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ct);
                        if (fileData != null)
                        {
                            freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                            freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFileContentType =
                                _contentTypeProvider.TryGetContentType(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out string? contentType)
                                    ? contentType : "application/octet-stream";
                        }
                    }

                    freeForumOutputViewModel.DetailBoardComments =
                        [.. await _boardService.GetCommentsByBoardIdAsync((long)boardId, ct)];
                    var detailNicknames = freeForumOutputViewModel.DetailBoardComments.Select(c => c.Writer)
                        .Append(freeBoard.Writer ?? "")
                        .Where(w => !string.IsNullOrEmpty(w))
                        .Select(w => w!)
                        .Distinct();
                    freeForumOutputViewModel.AllAccounts = await _accountService.GetAccountsByNicknamesAsync(detailNicknames, ct);
                    freeForumOutputViewModel.AdminNicknames = freeForumOutputViewModel.AllAccounts.ToAdminNicknameSet();

                    return View(freeForumOutputViewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in FreeForum action");
                    return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
                }
            }
            // Edit
            case "edit" when boardId == null:
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            case "edit":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    Method = method,
                    EditCurrentPage = page,
                    EditBoardId = (int)boardId
                };

                BoardResponse? freeBoard = await _boardService.GetBoardByIdAsync((long)boardId, EnumHelper.GetDescription(BoardType.FreeForum), ct);
                BoardAttachedFileDto? boardAttachedFile = await _boardService.GetAttachedFileByBoardIdAsync((long)boardId, ct);

                try
                {
                    if (freeBoard == null || freeBoard.Deleted)
                        return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                    // ViewBag.LoggedInAccount is re-fetched from the database by ViewBagPopulatorFilter
                    // on every request (not the session-cached snapshot), so a mid-session ownership-
                    // relevant change (e.g. nickname edit) is reflected immediately here.
                    AccountResponse loggedInAccount = ViewBag.LoggedInAccount;
                    if (!loggedInAccount.IsOwner(freeBoard.Writer))
                        return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                    (freeBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(freeBoard.Content ?? "", ct);

                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.View,
                        Content = freeBoard.Content,
                        Locked = freeBoard.Locked,
                        Updated = freeBoard.Updated,
                        BoardAttachedFileName = boardAttachedFile?.Name ?? "",
                        BoardAttachedFileExtension = boardAttachedFile?.Extension ?? "",
                        BoardAttachedFileSize = boardAttachedFile?.Size ?? 0,
                        BoardAttachedFilePath = boardAttachedFile?.Path ?? "",
                        IsImgTagIncluded = isImgTagIncluded
                    };

                    if (string.IsNullOrEmpty(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                    {
                        return View(freeForumOutputViewModel);
                    }

                    byte[]? fileData = await _boardService.DownloadFileAsync(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ct);
                    if (fileData == null)
                    {
                        return View(freeForumOutputViewModel);
                    }

                    freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                    freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFileContentType =
                        _contentTypeProvider.TryGetContentType(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out string? contentType)
                            ? contentType : "application/octet-stream";

                    return View(freeForumOutputViewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception in FreeForum action");
                    return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
                }
            }
            // List
            default:
            {
                const int pageSize = 5;
                if (page < 1) page = 1;

                var allFreeBoards = (await _boardService.GetBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum), ct)).ToList();
                var freeBoards = allFreeBoards.Where(m => !m.Noticed).OrderByDescending(a => a.Id).ToList();

                switch (searchType)
                {
                    case "Title" when !string.IsNullOrEmpty(searchText):
                    {
                        bool isAccountSessionExist = _sessionService.GetAccount() != null;
                        if (isAccountSessionExist)
                        {
                            AccountResponse loggedInAccount = _sessionService.GetAccount()!;
                            switch (loggedInAccount.Role)
                            {
                                // Administrator can search all posts, including locked ones
                                case Role.Admin:
                                    freeBoards = [.. freeBoards.Where(a => (a.Title ?? "").Contains(searchText))];
                                    break;
                                // User can search regular posts plus their own locked posts
                                case Role.User:
                                {
                                    List<BoardResponse> searchFreeBoards =
                                    [
                                        .. freeBoards.Where(a => (a.Title ?? "").Contains(searchText))
                                            .Where(a => !a.Locked),
                                        .. freeBoards.Where(a => (a.Title ?? "").Contains(searchText)).Where(a =>
                                            a.Locked && a.Writer == loggedInAccount.Nickname)
                                    ];
                                    freeBoards = searchFreeBoards;
                                    break;
                                }
                            }
                        }
                        else // When not logged in, locked posts should never appear in search results
                            freeBoards = [.. freeBoards.Where(a => (a.Title ?? "").Contains(searchText)).Where(a => !a.Locked)];
                        break;
                    }
                    case "Writer" when !string.IsNullOrEmpty(searchText):
                        freeBoards = [.. freeBoards.Where(a => (a.Writer ?? "").Contains(searchText))];
                        break;
                }

                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    NoticeBoards =
                        allFreeBoards.Where(m => m.Noticed)
                            .OrderByDescending(a => a.Id),
                    Method = "list",
                    Pager = new Pager(freeBoards.Count, page, pageSize)
                };
                freeForumOutputViewModel.Boards = [.. freeBoards.Skip((page - 1) * pageSize).Take(freeForumOutputViewModel.Pager.PageSize)];

                freeForumOutputViewModel.LoggedInAccount = _sessionService.GetAccount() ?? new AccountResponse { Role = Role.Anonymous };
                freeForumOutputViewModel.LoggedInAccountTimeZoneIanaId = freeForumOutputViewModel.LoggedInAccount.TimeZoneIanaId ?? "";
                var boardResponses = freeForumOutputViewModel.Boards as BoardResponse[] ?? [.. freeForumOutputViewModel.Boards];
                var listNicknames = freeForumOutputViewModel.NoticeBoards.Select(b => b.Writer)
                    .Concat(boardResponses.Select(b => b.Writer))
                    .Where(w => !string.IsNullOrEmpty(w))
                    .Select(w => w!)
                    .Distinct();
                freeForumOutputViewModel.AllAccounts = await _accountService.GetAccountsByNicknamesAsync(listNicknames, ct);
                freeForumOutputViewModel.AdminNicknames = freeForumOutputViewModel.AllAccounts.ToAdminNicknameSet();

                List<long> allBoardIds =
                [
                    .. freeForumOutputViewModel.NoticeBoards.Select(b => b.Id)
                        .Concat(boardResponses.Select(b => b.Id))
                        .Distinct()
                ];

                Dictionary<long, int> commentCounts = await _boardService.GetCommentCountsForBoardsAsync(allBoardIds, ct);
                Dictionary<long, BoardAttachedFileDto?> attachedFiles = await _boardService.GetAttachedFilesForBoardsAsync(allBoardIds, ct);

                foreach (var board in freeForumOutputViewModel.NoticeBoards)
                {
                    freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                    BoardAttachedFileDto? af = attachedFiles.GetValueOrDefault(board.Id);
                    freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
                }
                
                foreach (var board in boardResponses)
                {
                    freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                    BoardAttachedFileDto? af = attachedFiles.GetValueOrDefault(board.Id);
                    freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
                }

                freeForumOutputViewModel.SelectedSearchType = searchType;
                freeForumOutputViewModel.TypedSearchText = searchText;
                return View(freeForumOutputViewModel);
            }
        }
    }
    #endregion

    #region IsBoardExists
    /// <summary>Checks whether a board post with the given ID exists (used for edit validation).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsBoardExists(int id, CancellationToken ct)
    {
        try
        {
            BoardResponse? board = await _boardService.GetBoardByIdAsync(id, EnumHelper.GetDescription(BoardType.FreeForum), ct);
            if (board == null || board.Deleted)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            if (!board.Locked)
            {
                return Json(new { result = true, freeBoard = board });
            }

            // Re-fetched from the database by ViewBagPopulatorFilter on every request.
            AccountResponse loggedInAccount = ViewBag.LoggedInAccount;
            if (loggedInAccount.Nickname != board.Writer && loggedInAccount.Role != Role.Admin)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            return Json(new { result = true, freeBoard = board });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Update

    #region Edit
    /// <summary>Saves edits to an existing free-forum post.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> EditFreeBoard(BoardInputViewModel boardInputViewModel, CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to edit board."].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            BoardRequest boardRequest = new()
            {
                Id = boardInputViewModel.Id,
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Locked = boardInputViewModel.Locked
            };

            AttachedFileDto? attachedFile = await boardInputViewModel.UploadedFile.ToAttachedFileInfoAsync(ct);

            ServiceResult result = await _boardService.EditBoardAsync(boardRequest, loggedInAccount.Nickname!, attachedFile, ct);
            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully updated."].Value })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Delete

    #region Board
    /// <summary>Soft-deletes a free-forum post.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel, CancellationToken ct)
    {
        try
        {
            // Re-fetched from the database by ViewBagPopulatorFilter on every request.
            AccountResponse loggedInAccount = ViewBag.LoggedInAccount;

            ServiceResult result = await _boardService.DeleteBoardAsync(
                boardInputViewModel.Id,
                EnumHelper.GetDescription(BoardType.FreeForum),
                loggedInAccount.Nickname!,
                loggedInAccount.Role == Role.Admin,
                ct);

            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value })
                : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region BoardComment
    /// <summary>Soft-deletes a free-forum comment.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteComment(int id, CancellationToken ct)
    {
        try
        {
            // Re-fetched from the database by ViewBagPopulatorFilter on every request.
            AccountResponse loggedInAccount = ViewBag.LoggedInAccount;

            ServiceResult result = await _boardService.DeleteCommentAsync(
                id,
                loggedInAccount.Nickname!,
                loggedInAccount.Role == Role.Admin,
                EnumHelper.GetDescription(BoardType.FreeForum),
                ct);

            return result.Success
                ? Json(new { result = true })
                : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in forum action");
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #endregion
}
