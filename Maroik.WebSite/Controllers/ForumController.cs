using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages the free forum: listing, writing, editing, and deleting posts and comments,
/// plus Summernote inline-image uploads.
/// </summary>
public class ForumController : BaseController
{
    private readonly IRsaService _rsa;
    private readonly IHtmlLocalizer<ForumController> _localizer;
    private readonly ILogger<ForumController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IBoardService _boardService;
    private readonly IAccountService _accountService;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="ForumController"/> with the supplied dependencies.</summary>
    public ForumController(
        IHtmlLocalizer<ForumController> localizer,
        ILogger<ForumController> logger,
        IHostEnvironment hostEnvironment,
        IBoardService boardService,
        IAccountService accountService,
        IRsaService rsa,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
        : base(accountService, dashboardService, serverSettings, sessionService)
    {
        _rsa = rsa;
        _sessionService = sessionService;
        _localizer = localizer;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _boardService = boardService;
        _accountService = accountService;
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
    public async Task<IActionResult> WriteFreeBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            AccountResponse loginedAccount = _sessionService.GetAccount()!;

            if (loginedAccount.Role != Role.Admin && loginedAccount.Role != Role.User)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            BoardRequest boardRequest = new()
            {
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Writer = loginedAccount.Nickname!,
                Locked = boardInputViewModel.Locked,
                Noticed = loginedAccount.Role == Role.Admin && boardInputViewModel.Noticed
            };

            AttachedFileInfo? attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using MemoryStream ms = new();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileInfo
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            ServiceResult result = await _boardService.WriteBoardAsync(boardRequest, attachedFile);
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
    public async Task<IActionResult> WriteFreeComment([FromBody] BoardCommentInputViewModel boardCommentInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                return Json(new { result = false, error = _localizer["Please enter a boardComment."].Value });

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write boardComment."].Value });

            AccountResponse loginedAccount = _sessionService.GetAccount()!;

            BoardCommentRequest commentRequest = new()
            {
                BoardId = boardCommentInputViewModel.BoardId,
                AvatarImagePath = loginedAccount.AvatarImagePath!,
                Writer = loginedAccount.Nickname!,
                Content = boardCommentInputViewModel.Content
            };

            ServiceResult result = await _boardService.WriteCommentAsync(commentRequest);
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
    public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
    {
        if (summernoteImageFile == null)
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg, .jpeg or .png file allowed."].Value });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await summernoteImageFile.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        var file = new AttachedFileInfo
        {
            Bytes = bytes,
            ContentType = summernoteImageFile.ContentType,
            FileName = summernoteImageFile.FileName,
            Size = summernoteImageFile.Length
        };

        SummernoteUploadResult uploadResult = await _boardService.UploadSummernoteImageAsync(file, "Forum", EnumHelper.GetDescription(BoardType.FreeForum));

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
    public async Task<IActionResult> FreeForum(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "")
    {
        if (method == "write") // Write
        {
            FreeForumOutputViewModel freeForumOutputViewModel = new();

            #region Write board
            freeForumOutputViewModel.Method = method;
            #endregion

            bool isAccountSessionExist = _sessionService.GetAccount() != null; // Check if account session exists after login

            return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
        }
        else if (method == "detail") // Detail view
        {
            if (boardId == null)
            {
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            }

            FreeForumOutputViewModel freeForumOutputViewModel = new()
            {
                Method = method,
                DetailCurrentPage = page,
                DetailBoardId = boardId ?? default
            };

            BoardResponse? freeBoard = await _boardService.GetBoardByIdAsync(boardId ?? 0);
            BoardAttachedFileResponse? boardAttachedFile = await _boardService.GetAttachedFileByBoardIdAsync(boardId ?? 0);

            try
            {
                if (freeBoard == null || freeBoard.Deleted)
                    return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                if (freeBoard.Locked)
                {
                    AccountResponse loginedAccount = _sessionService.GetAccount()!; // Get Session

                    if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                    {
                        return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
                    }
                }

                await _boardService.IncrementBoardViewAsync(boardId ?? 0);

                (freeBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(freeBoard.Content ?? "");

                freeForumOutputViewModel.BoardOutputViewModel = new Maroik.WebSite.Models.ViewModels.Forum.BoardOutputViewModel()
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
                    IsImgTagIncluded = isImgTagIncluded
                };

                if (!string.IsNullOrEmpty(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                {
                    byte[]? fileData = await _boardService.DownloadFileAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                    if (fileData != null)
                    {
                        freeForumOutputViewModel!.BoardOutputViewModel.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = "application/octet-stream";
                    }
                }

                // Preload comments and accounts for detail view
                freeForumOutputViewModel!.DetailBoardComments = (await _boardService.GetCommentsByBoardIdAsync(boardId ?? 0))?.ToList() ?? [];
                var detailNicknames = freeForumOutputViewModel.DetailBoardComments.Select(c => c.Writer)
                    .Append(freeBoard.Writer ?? "")
                    .Where(w => !string.IsNullOrEmpty(w))
                    .Select(w => w!)
                    .Distinct();
                freeForumOutputViewModel.AllAccounts = await _accountService.GetAccountsByNicknamesAsync(detailNicknames) ?? [];
                freeForumOutputViewModel.LoginedAccount = _sessionService.GetAccount() ?? new AccountResponse { Role = Role.Anonymous };
                freeForumOutputViewModel.LoginedAccountTimeZoneIanaId = freeForumOutputViewModel.LoginedAccount.TimeZoneIanaId ?? "";

                return View(freeForumOutputViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in FreeForum action");
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            }
        }
        else if (method == "edit") // Edit
        {
            if (boardId == null)
            {
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            }

            FreeForumOutputViewModel freeForumOutputViewModel = new()
            {
                Method = method,
                EditCurrentPage = page,
                EditBoardId = boardId ?? default
            };

            BoardResponse? freeBoard = await _boardService.GetBoardByIdAsync(boardId ?? 0);
            BoardAttachedFileResponse? boardAttachedFile = await _boardService.GetAttachedFileByBoardIdAsync(boardId ?? 0);

            try
            {
                if (freeBoard == null || freeBoard.Deleted)
                    return RedirectToAction(nameof(BoardType.FreeForum), "Forum");

                AccountResponse loginedAccount = _sessionService.GetAccount()!;

                if (freeBoard.Writer != loginedAccount.Nickname) // If writer and logged-in user are not the same
                {
                    return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
                }

                (freeBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(freeBoard.Content ?? "");

                freeForumOutputViewModel.BoardOutputViewModel = new Maroik.WebSite.Models.ViewModels.Forum.BoardOutputViewModel()
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

                if (!string.IsNullOrEmpty(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                {
                    byte[]? fileData = await _boardService.DownloadFileAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                    if (fileData != null)
                    {
                        freeForumOutputViewModel!.BoardOutputViewModel.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = "application/octet-stream";
                    }
                }

                return View(freeForumOutputViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in FreeForum action");
                return RedirectToAction(nameof(BoardType.FreeForum), "Forum");
            }

        }
        else // List
        {
            #region Board default paging logic
            const int pageSize = 5;
            if (page < 1)
            {
                page = 1;
            }
            #endregion

            #region Logic to get all related board data
            IEnumerable<BoardResponse> allFreeBoards = await _boardService.GetBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            IEnumerable<BoardResponse> freeBoards = allFreeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
            #endregion

            #region Search keyword logic
            if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
            {
                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                if (isAccountSessionExist) // If logged in
                {
                    AccountResponse loginedAccount = _sessionService.GetAccount()!; // Get Session

                    if (loginedAccount.Role == Role.Admin) // Administrator can see all posts
                    {
                        freeBoards = freeBoards.Where(a => (a.Title ?? "").Contains(searchText));
                    }
                    else if (loginedAccount.Role == Role.User) // User can see regular posts + their own private posts
                    {
                        List<BoardResponse> searchFreeBoards = [];
                        List<BoardResponse> noLockedFreeBoards = freeBoards.Where(a => (a.Title ?? "").Contains(searchText)).Where(a => !a.Locked).ToList();
                        List<BoardResponse> myLockedFreeBoards = freeBoards.Where(a => (a.Title ?? "").Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                        searchFreeBoards.AddRange(noLockedFreeBoards);
                        searchFreeBoards.AddRange(myLockedFreeBoards);

                        freeBoards = searchFreeBoards;
                    }
                    else // If logged in but has neither Admin nor User role, it's a bug
                    {

                    }
                }
                else // If not logged in
                {
                    freeBoards = freeBoards.Where(a => (a.Title ?? "").Contains(searchText)).Where(a => !a.Locked); // When not logged in, private posts should not be searchable
                }
            }
            else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
            {
                freeBoards = freeBoards.Where(a => (a.Writer ?? "").Contains(searchText));
            }
            #endregion

            FreeForumOutputViewModel freeForumOutputViewModel = new();

            #region Notice
            freeForumOutputViewModel.NoticeBoards = allFreeBoards.Where(m => m.Deleted == false && m.Noticed == true).OrderByDescending(a => a.Id);
            #endregion

            #region Board list
            freeForumOutputViewModel.Method = "list";
            #endregion

            #region Board paging logic
            freeForumOutputViewModel.Pager = new Pager(freeBoards.Count(), page, pageSize);
            #endregion

            #region Board data logic
            freeForumOutputViewModel.Boards = freeBoards.Skip((page - 1) * pageSize).Take(freeForumOutputViewModel.Pager.PageSize).ToList();
            #endregion

            #region Preload comment counts and attached files for view
            freeForumOutputViewModel.LoginedAccount = _sessionService.GetAccount() ?? new AccountResponse { Role = Role.Anonymous };
            freeForumOutputViewModel.LoginedAccountTimeZoneIanaId = freeForumOutputViewModel.LoginedAccount.TimeZoneIanaId ?? "";
            var listNicknames = freeForumOutputViewModel.NoticeBoards.Select(b => b.Writer)
                .Concat(freeForumOutputViewModel.Boards.Select(b => b.Writer))
                .Where(w => !string.IsNullOrEmpty(w))
                .Select(w => w!)
                .Distinct();
            freeForumOutputViewModel.AllAccounts = await _accountService.GetAccountsByNicknamesAsync(listNicknames) ?? [];

            List<long> allBoardIds = freeForumOutputViewModel.NoticeBoards.Select(b => b.Id)
                .Concat(freeForumOutputViewModel.Boards.Select(b => b.Id))
                .Distinct().ToList();

            Dictionary<long, int> commentCounts = await _boardService.GetCommentCountsForBoardsAsync(allBoardIds);
            Dictionary<long, BoardAttachedFileResponse?> attachedFiles = await _boardService.GetAttachedFilesForBoardsAsync(allBoardIds);

            foreach (var board in freeForumOutputViewModel.NoticeBoards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                BoardAttachedFileResponse? af = attachedFiles.GetValueOrDefault(board.Id);
                freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
            }
            foreach (var board in freeForumOutputViewModel.Boards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                BoardAttachedFileResponse? af = attachedFiles.GetValueOrDefault(board.Id);
                freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
            }
            #endregion

            freeForumOutputViewModel.SelectedSearchType = searchType;
            freeForumOutputViewModel.TypedSearchText = searchText;
            return View(freeForumOutputViewModel);
        }
    }
    #endregion

    #region IsBoardExists
    /// <summary>Checks whether a board post with the given ID exists (used for edit validation).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsBoardExists(int id)
    {
        try
        {
            BoardResponse? board = await _boardService.GetBoardByIdAsync(id);

            return board == null || board.Deleted
                ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                : Json(new { result = true, freeBoard = board });
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
    public async Task<IActionResult> EditFreeBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });

            AccountResponse loginedAccount = _sessionService.GetAccount()!;

            BoardRequest boardRequest = new()
            {
                Id = boardInputViewModel.Id,
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Locked = boardInputViewModel.Locked
            };

            AttachedFileInfo? attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using MemoryStream ms = new();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileInfo
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            ServiceResult result = await _boardService.EditBoardAsync(boardRequest, loginedAccount.Nickname!, attachedFile);
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
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel)
    {
        try
        {
            AccountResponse loginedAccount = _sessionService.GetAccount()!;

            ServiceResult result = await _boardService.DeleteBoardAsync(
                boardInputViewModel.Id,
                EnumHelper.GetDescription(BoardType.FreeForum),
                loginedAccount.Nickname!,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value })
                : Json(new { result = false, error = result.ErrorKey });
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
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            AccountResponse loginedAccount = _sessionService.GetAccount()!;

            ServiceResult result = await _boardService.DeleteCommentAsync(
                id,
                loginedAccount.Nickname!,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true })
                : Json(new { result = false, error = result.ErrorKey });
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
