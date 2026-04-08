using Ganss.Xss;
using HtmlAgilityPack;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages the free forum: listing, writing, editing, and deleting posts and comments,
/// plus Summernote inline-image uploads.
/// </summary>
public class ForumController(
    IHtmlLocalizer<ForumController> localizer,
    IBoardService boardService,
    IAccountService accountService,
    IRsaService rsa,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    private readonly IAccountService _accountService = accountService;
    private readonly ISessionService _sessionService = sessionService;

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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            #region HtmlSanitizer
            var sanitizer = new HtmlSanitizer();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardInputViewModel.Content = sanitizer.Sanitize(boardInputViewModel.Content);
            #endregion

            #region Decrypt img tag file path
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(boardInputViewModel.Content);
            foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
            {
                var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", rsa.Decrypt(encryptedFilePath));
            }
            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100))
                return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });

            if (boardInputViewModel.Content.Length > 16384)
                return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });

            if (boardInputViewModel.UploadedFile != null)
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip")
                    return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760))
                    return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
            }

            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Please Login to write board"].Value });

            var loginedAccount = _sessionService.GetAccount()!;

            if (loginedAccount.Role != Role.Admin && loginedAccount.Role != Role.User)
                return Json(new { result = false, error = localizer["Please Login to write board"].Value });

            var boardRequest = new BoardRequest
            {
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Writer = loginedAccount.Nickname,
                Locked = boardInputViewModel.Locked,
                Noticed = loginedAccount.Role == Role.Admin && boardInputViewModel.Noticed
            };

            AttachedFileInfo attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using var ms = new MemoryStream();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileInfo
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            var result = await boardService.WriteBoardAsync(boardRequest, attachedFile);
            return result.Success
                ? Json(new { result = true, message = localizer["The board has been successfully created."].Value })
                : Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                return Json(new { result = false, error = localizer["Please enter a boardComment."].Value });

            #region HtmlSanitizer
            var sanitizer = new HtmlSanitizer();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardCommentInputViewModel.Content = sanitizer.Sanitize(boardCommentInputViewModel.Content);
            #endregion

            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Please Login to write boardComment."].Value });

            var loginedAccount = _sessionService.GetAccount()!;

            var commentRequest = new BoardCommentRequest
            {
                BoardId = boardCommentInputViewModel.BoardId,
                AvatarImagePath = loginedAccount.AvatarImagePath,
                Writer = loginedAccount.Nickname,
                Content = boardCommentInputViewModel.Content
            };

            var result = await boardService.WriteCommentAsync(commentRequest);
            return result.Success
                ? Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage })
                : Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return Ok(new { result = false, errorMessage = localizer["Only .jpg, .jpeg or .png file allowed."].Value });

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

        var uploadResult = await boardService.UploadSummernoteImageAsync(file, "Forum", EnumHelper.GetDescription(BoardType.FreeForum));

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        return Ok(new
        {
            result = true,
            file = File(uploadResult.FileBytes!, uploadResult.ContentType!, uploadResult.FileName!),
            filePath = rsa.Encrypt(uploadResult.FilePath!)
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
            var freeForumOutputViewModel = new FreeForumOutputViewModel();

            #region Write board
            freeForumOutputViewModel.Method = method;
            #endregion

            var isAccountSessionExist = _sessionService.GetAccount() != null; // Check if account session exists after login

            return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
        }
        else if (method == "detail") // Detail view
        {
            if (boardId == null)
            {
                return RedirectToAction("FreeForum", "Forum");
            }

            var freeForumOutputViewModel = new FreeForumOutputViewModel
            {
                Method = method,
                DetailCurrentPage = page,
                DetailBoardId = boardId ?? default
            };

            var freeBoard = await boardService.GetBoardByIdAsync(boardId ?? 0);
            var boardAttachedFile = await boardService.GetAttachedFileByBoardIdAsync(boardId ?? 0);

            try
            {
                if (freeBoard == null || freeBoard.Deleted)
                    return RedirectToAction("FreeForum", "Forum");

                if (freeBoard.Locked)
                {
                    var loginedAccount = _sessionService.GetAccount()!; // Get Session

                    if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }
                }

                await boardService.IncrementBoardViewAsync(boardId ?? 0);

                #region Encrypt img tag file path
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(freeBoard.Content);
                var isImgTagIncluded = false;

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    var filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    var fileData = await boardService.DownloadFileAsync(filePath);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        var provider = new FileExtensionContentTypeProvider();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        var base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", rsa.Encrypt(filePath));
                    }
                }

                freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;
                #endregion

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
                    IsImgTagIncluded = isImgTagIncluded
                };

                if (!string.IsNullOrEmpty(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                {
                    var fileData = await boardService.DownloadFileAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        var provider = new FileExtensionContentTypeProvider();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = contentType;
                    }
                }

                // Preload comments and accounts for detail view
                freeForumOutputViewModel.DetailBoardComments = (await boardService.GetCommentsByBoardIdAsync(boardId ?? 0))?.ToList() ?? [];
                freeForumOutputViewModel.AllAccounts = await _accountService.GetAllAccountsAsync() ?? [];
                freeForumOutputViewModel.LoginedAccount = _sessionService.GetAccount() ?? new AccountResponse { Role = Role.Anonymous };
                freeForumOutputViewModel.LoginedAccountTimeZoneIanaId = freeForumOutputViewModel.LoginedAccount.TimeZoneIanaId ?? "";

                return View(freeForumOutputViewModel);
            }
            catch
            {
                return RedirectToAction("FreeForum", "Forum");
            }
        }
        else if (method == "edit") // Edit
        {
            if (boardId == null)
            {
                return RedirectToAction("FreeForum", "Forum");
            }

            var freeForumOutputViewModel = new FreeForumOutputViewModel
            {
                Method = method,
                EditCurrentPage = page,
                EditBoardId = boardId ?? default
            };

            var freeBoard = await boardService.GetBoardByIdAsync(boardId ?? 0);
            var boardAttachedFile = await boardService.GetAttachedFileByBoardIdAsync(boardId ?? 0);

            try
            {
                if (freeBoard == null || freeBoard.Deleted)
                    return RedirectToAction("FreeForum", "Forum");

                var loginedAccount = _sessionService.GetAccount()!;

                if (freeBoard.Writer != loginedAccount.Nickname) // If writer and logged-in user are not the same
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

                #region Encrypt img tag file path
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(freeBoard.Content);
                var isImgTagIncluded = false;

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    var filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    var fileData = await boardService.DownloadFileAsync(filePath);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        var provider = new FileExtensionContentTypeProvider();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        var base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", rsa.Encrypt(filePath));
                    }
                }

                freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;
                #endregion

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

                if (!string.IsNullOrEmpty(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                {
                    var fileData = await boardService.DownloadFileAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        var provider = new FileExtensionContentTypeProvider();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = contentType;
                    }
                }

                return View(freeForumOutputViewModel);
            }
            catch
            {
                return RedirectToAction("FreeForum", "Forum");
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
            var allFreeBoards = await boardService.GetBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            var freeBoards = allFreeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id).ToList();
            #endregion

            #region Search keyword logic
            if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
            {
                var isAccountSessionExist = _sessionService.GetAccount() != null;
                if (isAccountSessionExist) // If logged in
                {
                    var loginedAccount = _sessionService.GetAccount()!; // Get Session

                    if (loginedAccount.Role == Role.Admin) // Administrator can see all posts
                    {
                        freeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).ToList();
                    }
                    else if (loginedAccount.Role == Role.User) // User can see regular posts + their own private posts
                    {
                        var searchFreeBoards = new List<BoardResponse>();
                        var noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                        var myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
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
                    freeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList(); // When not logged in, private posts should not be searchable
                }
            }
            else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
            {
                freeBoards = freeBoards.Where(a => a.Writer.Contains(searchText)).ToList();
            }
            #endregion

            var freeForumOutputViewModel = new FreeForumOutputViewModel();

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
            freeForumOutputViewModel.AllAccounts = await _accountService.GetAllAccountsAsync() ?? [];

            var allBoardIds = freeForumOutputViewModel.NoticeBoards.Select(b => b.Id)
                .Concat(freeForumOutputViewModel.Boards.Select(b => b.Id))
                .Distinct().ToList();

            var commentCounts = await boardService.GetCommentCountsForBoardsAsync(allBoardIds);
            var attachedFiles = await boardService.GetAttachedFilesForBoardsAsync(allBoardIds);

            foreach (var board in freeForumOutputViewModel.NoticeBoards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                var af = attachedFiles.GetValueOrDefault(board.Id);
                freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
            }
            foreach (var board in freeForumOutputViewModel.Boards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                var af = attachedFiles.GetValueOrDefault(board.Id);
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
            var board = await boardService.GetBoardByIdAsync(id);

            return board == null || board.Deleted
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : Json(new { result = true, freeBoard = board });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            #region HtmlSanitizer
            var sanitizer = new HtmlSanitizer();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardInputViewModel.Content = sanitizer.Sanitize(boardInputViewModel.Content);
            #endregion

            #region Decrypt img tag file path
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(boardInputViewModel.Content);
            foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
            {
                var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", rsa.Decrypt(encryptedFilePath));
            }
            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100))
                return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });

            if (boardInputViewModel.Content.Length > 16384)
                return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });

            if (boardInputViewModel.UploadedFile != null)
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip")
                    return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760))
                    return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
            }

            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Please Login to edit board"].Value });

            var loginedAccount = _sessionService.GetAccount()!;

            var boardRequest = new BoardRequest
            {
                Id = boardInputViewModel.Id,
                Type = EnumHelper.GetDescription(BoardType.FreeForum),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Locked = boardInputViewModel.Locked
            };

            AttachedFileInfo attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using var ms = new MemoryStream();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileInfo
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            var result = await boardService.EditBoardAsync(boardRequest, loginedAccount.Nickname, attachedFile);
            return result.Success
                ? Json(new { result = true, message = localizer["The board has been successfully updated."].Value })
                : Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            var loginedAccount = _sessionService.GetAccount()!;

            var result = await boardService.DeleteBoardAsync(
                boardInputViewModel.Id,
                EnumHelper.GetDescription(BoardType.FreeForum),
                loginedAccount.Nickname,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true, message = localizer["The board has been successfully deleted."].Value })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            var loginedAccount = _sessionService.GetAccount()!;

            var result = await boardService.DeleteCommentAsync(
                id,
                loginedAccount.Nickname,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion
}
