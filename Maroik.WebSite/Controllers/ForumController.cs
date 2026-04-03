using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Extensions;
using Maroik.Core.Common.Utilities;
using Maroik.Core.Client.Contracts;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class ForumController : Controller
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
    private readonly ApplicationDbContext _context;

    public ForumController(IHtmlLocalizer<ForumController> localizer, ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment, IBoardAttachedFileRepository boardAttachedFileRepository, IAccountRepository accountRepository, IBoardCommentRepository boardCommentRepository, IFileClient fileClient, ApplicationDbContext context)
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
        _context = context;
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
            if (ModelState.IsValid)
            {
                boardInputViewModel.Content ??= ""; // If no content was entered

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;
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

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                }

                if (boardInputViewModel.UploadedFile != null) // If a file was attached
                {
                    if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                        Board board = new();

                        if (loginedAccount.Role == Role.Admin) // If administrator
                        {
                            board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // Free forum
                            board.Title = boardInputViewModel.Title;
                            board.Content = boardInputViewModel.Content;
                            board.Writer = loginedAccount.Nickname;
                            board.Created = DateTime.UtcNow;
                            board.Updated = DateTime.UtcNow;
                            board.View = 0;
                            board.Deleted = false;
                            board.Locked = boardInputViewModel.Locked;
                            board.Noticed = boardInputViewModel.Noticed;
                        }
                        else if (loginedAccount.Role == Role.User) // If regular user
                        {
                            board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // Free forum
                            board.Title = boardInputViewModel.Title;
                            board.Content = boardInputViewModel.Content;
                            board.Writer = loginedAccount.Nickname;
                            board.Created = DateTime.UtcNow;
                            board.Updated = DateTime.UtcNow;
                            board.View = 0;
                            board.Deleted = false;
                            board.Locked = boardInputViewModel.Locked;
                        }
                        else // If non-logged in user
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
                        }

                        await _boardRepository.WriteBoardAsync(board);

                        #region Save attached file
                        if (boardInputViewModel.UploadedFile != null) // If attached file exists
                        {
                            if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                var createdBoardId = board.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                                _ = await _fileClient.UploadAsync(boardInputViewModel.UploadedFile, filePath, ServerSetting.FileStorageBaseUrl);

                                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                {
                                    BoardId = createdBoardId,
                                    Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                    Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                    Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                    Path = $"upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}"
                                });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                            }

                        }
                        #endregion
                        await transaction.CommitAsync();
                        return Json(new { result = true, message = _localizer["The board has been successfully created."].Value });
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
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
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                {
                    return Json(new { result = false, error = _localizer["Please enter a boardComment."].Value });
                }

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardCommentInputViewModel.Content);
                boardCommentInputViewModel.Content = sanitized;
                #endregion

                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        IEnumerable<Board> freeForumBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                        freeForumBoards = freeForumBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                        Board freeForumBoard = freeForumBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                        if (freeForumBoard.Id != boardCommentInputViewModel.BoardId)
                        {
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }

                        IEnumerable<BoardComment> totalBoardCommentsInSelectedBoard = await _boardCommentRepository.GetOneBoardCommentsAsync(boardCommentInputViewModel.BoardId);

                        BoardComment boardComment = new()
                        {
                            BoardId = boardCommentInputViewModel.BoardId,
                            Order = totalBoardCommentsInSelectedBoard?.Count() ?? default,
                            AvatarImagePath = loginedAccount.AvatarImagePath,
                            Writer = loginedAccount.Nickname,
                            Content = boardCommentInputViewModel.Content,
                            Created = DateTime.UtcNow,
                            Deleted = false
                        };
                        await _boardCommentRepository.WriteBoardCommentAsync(boardComment);

                        return Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage });
                    }
                    else
                    {
                        return Json(new { result = false, error = _localizer["Please Login to write boardComment."].Value });
                    }
                }
                catch
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
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
            Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account),
            out byte[] resultByte);

        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte));
        
        string summernoteImagePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "summernote", "images");
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine(summernoteImagePath, imageFile);

        var uploadResult = await _fileClient.UploadAsync(summernoteImageFile, filePath, ServerSetting.FileStorageBaseUrl);

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

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out _); // Check if account session exists after login

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

            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                Board freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                if (freeBoard.Locked)
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte); // If writer or administrator
                    Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                    if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }
                }

                freeBoard.View++;
                await _boardRepository.UpdateBoardAsync(freeBoard);
                BoardAttachedFile boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

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

            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                Board freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                _ = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (freeBoard.Writer != loginedAccount.Nickname) // If writer and logged-in user are not the same
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

                BoardAttachedFile boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

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
            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
            #endregion

            #region Search keyword logic
            if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
            {
                bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);

                if (isAccountSessionExist) // If logged in
                {
                    Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                    if (loginedAccount.Role == Role.Admin) // Administrator can see all posts
                    {
                        freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                    }
                    else if (loginedAccount.Role == Role.User) // User can see regular posts + their own private posts
                    {
                        List<Board> searchFreeBoards = [];
                        List<Board> noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                        List<Board> myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
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

            ViewBag.SelectedSearchType = searchType;
            ViewBag.TypedSearchText = searchText;
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
            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            if (freeBoards == null)
            {
                return Json(new { result = false, error = _localizer["No freeBoard exists"].Value });
            }
            else
            {
                Board tempFreeBoard = freeBoards.Where(a => a.Id == id).FirstOrDefault();

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
            if (ModelState.IsValid)
            {
                boardInputViewModel.Content ??= ""; // If no content was entered

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;
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

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                }

                if (boardInputViewModel.UploadedFile != null) // If a file was attached
                {
                    if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                        freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                        Board tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                        if (tempFreeBoard.Writer != loginedAccount.Nickname)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }

                        tempFreeBoard.Title = boardInputViewModel.Title;
                        tempFreeBoard.Content = boardInputViewModel.Content;
                        tempFreeBoard.Locked = boardInputViewModel.Locked;
                        tempFreeBoard.Updated = DateTime.UtcNow;

                        await _boardRepository.UpdateBoardAsync(tempFreeBoard);

                        List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository?.GetAllAsync();
                        BoardAttachedFile previousBoardAttachedFile = boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                        #region Save attached file

                        #region Existing attached file exists && New attached file exists
                        if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                int createdBoardId = boardInputViewModel.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                                _ = await _fileClient.UploadAsync(boardInputViewModel.UploadedFile, filePath, ServerSetting.FileStorageBaseUrl);

                                previousBoardAttachedFile.Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                previousBoardAttachedFile.Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Path = $"upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}";

                                await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);
                                await transaction.CommitAsync();
                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                            }
                        }
                        #endregion

                        #region Existing attached file exists && New attached file does not exist
                        else if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                        {
                            previousBoardAttachedFile.Size = 0;
                            previousBoardAttachedFile.Name = "";
                            previousBoardAttachedFile.Extension = "";
                            previousBoardAttachedFile.Path = "";

                            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);
                            await transaction.CommitAsync();
                            return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                        }
                        #endregion

                        #region Existing attached file does not exist && New attached file exists
                        else if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                int createdBoardId = boardInputViewModel.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                                _ = await _fileClient.UploadAsync(boardInputViewModel.UploadedFile, filePath, ServerSetting.FileStorageBaseUrl);

                                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                {
                                    BoardId = createdBoardId,
                                    Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                    Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                    Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                    Path = $"upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}"
                                });
                                await transaction.CommitAsync();
                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                            }
                        }
                        #endregion

                        #region Existing attached file does not exist && New attached file does not exist
                        else
                        {
                            await transaction.CommitAsync();
                            return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                        }
                        #endregion

                        #endregion
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
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
            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            if (freeBoards == null)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                Board tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                if (tempFreeBoard == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);
                    Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if ((loginedAccount.Nickname == tempFreeBoard.Writer) || loginedAccount.Role == Role.Admin) // If visitor is writer or administrator
                    {
                        #region Delete board
                        tempFreeBoard.Deleted = true;
                        await _boardRepository.DeleteBoardAsync(tempFreeBoard);
                        #endregion

                        return Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value });
                    }
                    else
                    {
                        return Json(new { result = true, message = _localizer["You do not have permission to delete."].Value });
                    }
                }
            }
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
            BoardComment boardComment = await _boardCommentRepository.GetBoardCommentAsync(id);

            if (boardComment == null)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Core.Common.Utilities.EnumHelper.GetDescription(Maroik.Core.Common.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if ((loginedAccount.Nickname == boardComment.Writer) || loginedAccount.Role == Role.Admin) // If deleter is writer or administrator
                {
                    #region Delete board comment
                    boardComment.Deleted = true;
                    await _boardCommentRepository.DeleteBoardCommentAsync(boardComment);
                    #endregion

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = true, message = _localizer["You do not have permission to delete."].Value });
                }

            }
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
