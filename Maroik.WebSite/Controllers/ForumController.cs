using Mapster;
﻿using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class ForumController : BaseController
{
    private readonly RSA _rsa;
    private readonly IHtmlLocalizer<ForumController> _localizer;
    private readonly ILogger<ForumController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IBoardCommentRepository _boardCommentRepository;
    private readonly IFileClient _fileClient;
    private readonly IBoardService _boardService;

    public ForumController(IHtmlLocalizer<ForumController> localizer, ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment, IBoardAttachedFileRepository boardAttachedFileRepository, IAccountRepository accountRepository, IBoardCommentRepository boardCommentRepository, IFileClient fileClient, IBoardService boardService)
    {
        _rsa = new RSA(ServerSetting.RSAAlgorithm, Encoding.UTF8, ServerSetting.RSAPrivateKey, ServerSetting.RSAPublicKey);
        _localizer = localizer;
        _logger = logger;
        _boardRepository = boardRepository;
        _hostEnvironment = hostEnvironment;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _accountRepository = accountRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _boardService = boardService;
    }

    #region FreeForum

    #region Create

    #region Write

    #region FreeBoard
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

            #region HtmlSanitizer
            HtmlSanitizer sanitizer = new();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardInputViewModel.Content = sanitizer.Sanitize(boardInputViewModel.Content);
            #endregion

            #region Decrypt img tag file path
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(boardInputViewModel.Content);
            foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
            {
                string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
            }
            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100))
                return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });

            if (boardInputViewModel.Content.Length > 16384)
                return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });

            if (boardInputViewModel.UploadedFile != null)
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip")
                    return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760))
                    return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
            }

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
                out byte[] resultByte);

            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));

            if (loginedAccount.Role != Role.Admin && loginedAccount.Role != Role.User)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            BoardRequest boardRequest = new()
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
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region FreeComment
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

            #region HtmlSanitizer
            HtmlSanitizer sanitizer = new();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardCommentInputViewModel.Content = sanitizer.Sanitize(boardCommentInputViewModel.Content);
            #endregion

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
                out byte[] resultByte);

            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write boardComment."].Value });

            AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));

            IEnumerable<BoardResponse> freeForumBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            BoardResponse freeForumBoard = freeForumBoards.Where(m => !m.Deleted).FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);
            if (freeForumBoard?.Id != boardCommentInputViewModel.BoardId)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            BoardCommentRequest commentRequest = new()
            {
                BoardId = boardCommentInputViewModel.BoardId,
                AvatarImagePath = loginedAccount.AvatarImagePath,
                Writer = loginedAccount.Nickname,
                Content = boardCommentInputViewModel.Content
            };

            ServiceResult result = await _boardService.WriteCommentAsync(commentRequest);
            return result.Success
                ? Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Summernote Image File Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
    {
        if (summernoteImageFile == null)
        {
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
        }

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
        }

        // Extension check
        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg, .jpeg or .png file allowed."].Value });
        }

        try
        {
            // Check if it's an actual image (Magick.NET)
            using var image = new MagickImage(summernoteImageFile.OpenReadStream());
            // If Magick.NET throws an exception, it's not an image or an unsupported format
        }
        catch (MagickException)
        {
            return Ok(new { result = false, errorMessage = _localizer["Invalid image file."].Value });
        }

        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
            out byte[] resultByte);

        var loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));
        
        string summernoteImagePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "summernote", "images");
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine(summernoteImagePath, imageFile);

        bool uploadResult;
        using (MemoryStream ms = new())
        {
            await summernoteImageFile.CopyToAsync(ms);
            uploadResult = await _fileClient.UploadAsync(ms.ToArray(), summernoteImageFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl);
        }

        if (uploadResult)
        {
            var fileStream = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);
            return Ok(new
            {
                result = true,
                file = File(fileStream, summernoteImageFile.ContentType, imageFile),
                filePath = _rsa.Encrypt(filePath)
            });
        }
        else
        {
            return Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write
    public async Task<IActionResult> FreeForum(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "")
    {
        if (method == "write") // Write
        {
            FreeForumOutputViewModel freeForumOutputViewModel = new();

            #region Write board
            freeForumOutputViewModel.Method = method;
            #endregion

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out _); // Check if account session exists after login

            return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
        }
        else if (method == "detail") // Detail view
        {
            if (boardId == null)
            {
                return RedirectToAction("FreeForum", "Forum");
            }

            FreeForumOutputViewModel freeForumOutputViewModel = new()
            {
                Method = method,
                DetailCurrentPage = page,
                DetailBoardId = boardId ?? default
            };

            IEnumerable<BoardResponse> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFileResponse> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                BoardResponse freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                if (freeBoard.Locked)
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte); // If writer or administrator
                    AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)); // Get Session

                    if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }
                }

                freeBoard.View++;
                await _boardRepository.UpdateBoardAsync(freeBoard.Adapt<BoardRequest>());
                BoardAttachedFileResponse boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                #region Encrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(freeBoard.Content);
                bool isImgTagIncluded = false;

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    string filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    byte[] fileData = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        string base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }
                }

                freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;
                #endregion

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
                    byte[] fileData = await _fileClient.DownloadAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = contentType;
                    }
                }

                // Preload comments and accounts for detail view
                freeForumOutputViewModel.DetailBoardComments = (await _boardCommentRepository.GetOneBoardCommentsAsync(boardId ?? 0))?.ToList() ?? [];
                freeForumOutputViewModel.AllAccounts = await _accountRepository.GetAllAsync() ?? [];
                _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] detailSessionByte);
                freeForumOutputViewModel.LoginedAccount = detailSessionByte?.Length > 0
                    ? JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(detailSessionByte)) ?? new AccountResponse { Role = Role.Anonymous }
                    : new AccountResponse { Role = Role.Anonymous };
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

            FreeForumOutputViewModel freeForumOutputViewModel = new()
            {
                Method = method,
                EditCurrentPage = page,
                EditBoardId = boardId ?? default
            };

            IEnumerable<BoardResponse> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFileResponse> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                BoardResponse freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

                if (freeBoard.Writer != loginedAccount.Nickname) // If writer and logged-in user are not the same
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

                BoardAttachedFileResponse boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                #region Encrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(freeBoard.Content);
                bool isImgTagIncluded = false;

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    string filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    byte[] fileData = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        string base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }
                }

                freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;
                #endregion

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
                    byte[] fileData = await _fileClient.DownloadAsync(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(freeForumOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out string contentType))
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
            IEnumerable<BoardResponse> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
            #endregion

            #region Search keyword logic
            if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
            {
                bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                if (isAccountSessionExist) // If logged in
                {
                    AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)); // Get Session

                    if (loginedAccount.Role == Role.Admin) // Administrator can see all posts
                    {
                        freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                    }
                    else if (loginedAccount.Role == Role.User) // User can see regular posts + their own private posts
                    {
                        List<BoardResponse> searchFreeBoards = [];
                        List<BoardResponse> noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                        List<BoardResponse> myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
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
                    freeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked); // When not logged in, private posts should not be searchable
                }
            }
            else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
            {
                freeBoards = freeBoards.Where(a => a.Writer.Contains(searchText));
            }
            #endregion

            FreeForumOutputViewModel freeForumOutputViewModel = new();

            #region Notice
            freeForumOutputViewModel.NoticeBoards = (await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum))).Where(m => m.Deleted == false && m.Noticed == true).OrderByDescending(a => a.Id);
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
            _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] sessionByte);
            freeForumOutputViewModel.LoginedAccount = sessionByte?.Length > 0
                ? JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(sessionByte)) ?? new AccountResponse { Role = Role.Anonymous }
                : new AccountResponse { Role = Role.Anonymous };
            freeForumOutputViewModel.LoginedAccountTimeZoneIanaId = freeForumOutputViewModel.LoginedAccount.TimeZoneIanaId ?? "";
            freeForumOutputViewModel.AllAccounts = await _accountRepository.GetAllAsync() ?? [];
            List<BoardAttachedFileResponse> allAttachedFiles = await _boardAttachedFileRepository.GetAllAsync() ?? [];
            foreach (var board in freeForumOutputViewModel.NoticeBoards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] =
                    (await _boardCommentRepository.GetOneBoardCommentsAsync(board.Id))?.Count(c => !c.Deleted) ?? 0;
                freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] =
                    allAttachedFiles.Where(f => f.BoardId == board.Id && !string.IsNullOrEmpty(f.Name)).ToList();
            }
            foreach (var board in freeForumOutputViewModel.Boards)
            {
                freeForumOutputViewModel.CommentCountByBoardId[board.Id] =
                    (await _boardCommentRepository.GetOneBoardCommentsAsync(board.Id))?.Count(c => !c.Deleted) ?? 0;
                freeForumOutputViewModel.AttachedFilesByBoardId[board.Id] =
                    allAttachedFiles.Where(f => f.BoardId == board.Id && !string.IsNullOrEmpty(f.Name)).ToList();
            }
            #endregion

            freeForumOutputViewModel.SelectedSearchType = searchType;
            freeForumOutputViewModel.TypedSearchText = searchText;
            return View(freeForumOutputViewModel);
        }
    }
    #endregion

    #region IsBoardExists
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsBoardExists(int id)
    {
        try
        {
            IEnumerable<BoardResponse> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            if (freeBoards == null)
            {
                return Json(new { result = false, error = _localizer["No freeBoard exists"].Value });
            }
            else
            {
                BoardResponse tempFreeBoard = freeBoards.Where(a => a.Id == id).FirstOrDefault();

                return tempFreeBoard == null
                    ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                    : (IActionResult)Json(new { result = true, freeBoard = tempFreeBoard });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Update

    #region Edit
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

            #region HtmlSanitizer
            HtmlSanitizer sanitizer = new();
            _ = sanitizer.AllowedAttributes.Add("class");
            boardInputViewModel.Content = sanitizer.Sanitize(boardInputViewModel.Content);
            #endregion

            #region Decrypt img tag file path
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(boardInputViewModel.Content);
            foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
            {
                string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
            }
            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100))
                return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });

            if (boardInputViewModel.Content.Length > 16384)
                return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });

            if (boardInputViewModel.UploadedFile != null)
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip")
                    return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760))
                    return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
            }

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
                out byte[] resultByte);

            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });

            AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));

            BoardRequest boardRequest = new()
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

            ServiceResult result = await _boardService.EditBoardAsync(boardRequest, loginedAccount.Nickname, attachedFile);
            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully updated."].Value })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Delete

    #region Board
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
                out byte[] resultByte);
            AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            ServiceResult result = await _boardService.DeleteBoardAsync(
                boardInputViewModel.Id,
                EnumHelper.GetDescription(BoardType.FreeForum),
                loginedAccount.Nickname,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region BoardComment
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
                out byte[] resultByte);
            AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            ServiceResult result = await _boardService.DeleteCommentAsync(
                id,
                loginedAccount.Nickname,
                loginedAccount.Role == Role.Admin);

            return result.Success
                ? Json(new { result = true })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #endregion

}
