using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class ForumController(
    IHtmlLocalizer<ForumController> localizer,
    IBoardRepository boardRepository,
    IBoardAttachedFileRepository boardAttachedFileRepository,
    IAccountRepository accountRepository,
    IBoardCommentRepository boardCommentRepository,
    IFileRepository fileRepository,
    ApplicationDbContext context)
    : Controller
{
    private readonly Rsa _rsa = new(RsaType.Rsa2, Encoding.UTF8, Rsa.PrivateKey, Rsa.PublicKey);

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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

            #region HtmlSanitizer
            HtmlSanitizer sanitizer = new();
            _ = sanitizer.AllowedAttributes.Add("class");
            var sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
            boardInputViewModel.Content = sanitized;
            #endregion

            #region Decrypt img tag file path
            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(boardInputViewModel.Content);

            foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
            {
                var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
            }

            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
            {
                return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });
            }

            if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
            {
                return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });
            }

            if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip") // zip 확장자만 가능
                {
                    return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                }

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                {
                    return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                }
            }
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

                if (!isAccountSessionExist)
                    return Json(new { result = false, error = localizer["Please Login to write board"].Value });
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                Board board = new();

                switch (loginedAccount.Role)
                {
                    // 관리자인 경우
                    case Role.Admin:
                        board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // 자유게시판
                        board.Title = boardInputViewModel.Title;
                        board.Content = boardInputViewModel.Content;
                        board.Writer = loginedAccount.Nickname;
                        board.Created = DateTime.UtcNow;
                        board.Updated = DateTime.UtcNow;
                        board.View = 0;
                        board.Deleted = false;
                        board.Locked = boardInputViewModel.Locked;
                        board.Noticed = boardInputViewModel.Noticed;
                        break;
                    // 일반 사용자인 경우
                    case Role.User:
                        board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // 자유게시판
                        board.Title = boardInputViewModel.Title;
                        board.Content = boardInputViewModel.Content;
                        board.Writer = loginedAccount.Nickname;
                        board.Created = DateTime.UtcNow;
                        board.Updated = DateTime.UtcNow;
                        board.View = 0;
                        board.Deleted = false;
                        board.Locked = boardInputViewModel.Locked;
                        break;
                    // 비로그인 사용자인 경우
                    default:
                        return Json(new { result = false, error = localizer["Please Login to write board"].Value });
                }

                await boardRepository.WriteBoardAsync(board);

                #region 첨부 파일 저장
                if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                {
                    if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                    {
                        var createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                        var boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                        var guid = Guid.NewGuid().ToString().ToUpper();
                        var boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                        var filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                        _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                        await boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile
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
                        return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                    }

                }
                #endregion
                await transaction.CommitAsync();
                return Json(new { result = true, message = localizer["The board has been successfully created."].Value });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                    return Json(new { result = false, error = localizer["Please enter a boardComment."].Value });
                }

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                var sanitized = sanitizer.Sanitize(boardCommentInputViewModel.Content);
                boardCommentInputViewModel.Content = sanitized;
                #endregion

                try
                {
                    var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

                    if (isAccountSessionExist)
                    {
                        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        var freeForumBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                        freeForumBoards = freeForumBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                        var freeForumBoard = freeForumBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                        if (freeForumBoard != null && freeForumBoard.Id != boardCommentInputViewModel.BoardId)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        var totalBoardCommentsInSelectedBoard = await boardCommentRepository.GetOneBoardCommentsAsync(boardCommentInputViewModel.BoardId);

                        BoardComment boardComment = new()
                        {
                            BoardId = boardCommentInputViewModel.BoardId,
                            Order = totalBoardCommentsInSelectedBoard?.Count() ?? 0,
                            AvatarImagePath = loginedAccount.AvatarImagePath,
                            Writer = loginedAccount.Nickname,
                            Content = boardCommentInputViewModel.Content,
                            Created = DateTime.UtcNow,
                            Deleted = false
                        };
                        await boardCommentRepository.WriteBoardCommentAsync(boardComment);

                        return Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage });
                    }

                    return Json(new { result = false, error = localizer["Please Login to write boardComment."].Value });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });
        }

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
        }

        // 확장자 검사
        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            return Ok(new { result = false, errorMessage = localizer["Only .jpg, .jpeg or .png file allowed."].Value });
        }

        try
        {
            // 실제 이미지 여부 검사 (Magick.NET)
            using var image = new MagickImage(summernoteImageFile.OpenReadStream());
            // Magick.NET에서 예외가 발생하면 이미지가 아니거나 허용되지 않는 포맷임
        }
        catch (MagickException)
        {
            return Ok(new { result = false, errorMessage = localizer["Invalid image file."].Value });
        }
            
        var summernoteImagePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "summernote", "images");
        var imageFile = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(summernoteImagePath, imageFile);

        var uploadResult = await fileRepository.UploadAsync(summernoteImageFile, filePath);

        if (!uploadResult) return Ok(new { result = false, errorMessage = localizer["Input is invalid"].Value });
        var fileStream = await fileRepository.DownloadAsync(filePath);
        return Ok(new
        {
            result = true,
            file = File(fileStream, summernoteImageFile.ContentType, imageFile),
            filePath = _rsa.Encrypt(filePath)
        });

    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write
    public async Task<IActionResult> FreeForum(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "")
    {
        switch (method)
        {
            // 글쓰기 (write)
            case "write":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new();

                #region 게시판 글쓰기
                freeForumOutputViewModel.Method = method;
                #endregion

                var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
            }
            // 상세보기 (detail)
            case "detail" when boardId == null:
                return RedirectToAction("FreeForum", "Forum");
            case "detail":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    Method = method,
                    DetailCurrentPage = page,
                    DetailBoardId = (int)boardId
                };

                var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var boardAttachedFiles = await boardAttachedFileRepository.GetAllAsync();

                try
                {
                    var freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    if (freeBoard is { Locked: true })
                    {
                        _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte); // 작성자 또는 관리자인 경우
                        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])); // Get Session

                        if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                        {
                            return RedirectToAction("FreeForum", "Forum");
                        }
                    }

                    if (freeBoard != null)
                    {
                        freeBoard.View++;
                        await boardRepository.UpdateBoardAsync(freeBoard);
                        var boardAttachedFile =
                            boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                        #region Encrypt img tag file path

                        HtmlDocument htmlDocument = new();
                        htmlDocument.LoadHtml(freeBoard.Content);
                        var isImgTagIncluded = false;

                        foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                        {
                            isImgTagIncluded = true;
                            var filePath = imgTag.GetAttributeValue("alt", "");

                            // 파일을 다운로드하는 메서드
                            var fileData = await fileRepository.DownloadAsync(filePath);

                            if (fileData == null) continue;
                            // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                            FileExtensionContentTypeProvider provider = new();

                            // 기본 contentType은 "application/octet-stream"으로 설정
                            if (!provider.TryGetContentType(filePath, out var contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // 파일 데이터를 Base64로 변환
                            var base64Data = Convert.ToBase64String(fileData);

                            // img 태그에 base64 데이터 저장 (src에 넣는 대신 data-file 속성 사용)
                            _ = imgTag.SetAttributeValue("data-file", base64Data);
                            _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                            _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                        }

                        freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;

                        #endregion

                        freeForumOutputViewModel.BoardOutputViewModel =
                            new BoardOutputViewModel
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
                    }

                    if (string.IsNullOrEmpty(
                            freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                        return View(freeForumOutputViewModel);
                    {
                        var fileData = await fileRepository.DownloadAsync(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                        if (fileData == null) return View(freeForumOutputViewModel);
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        if (freeForumOutputViewModel.BoardOutputViewModel == null)
                            return View(freeForumOutputViewModel);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType =
                            contentType;
                    }

                    return View(freeForumOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("FreeForum", "Forum");
                }
            }
            // 수정 (edit)
            case "edit" when boardId == null:
                return RedirectToAction("FreeForum", "Forum");
            case "edit":
            {
                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    Method = method,
                    EditCurrentPage = page,
                    EditBoardId = (int)boardId
                };

                var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var boardAttachedFiles = await boardAttachedFileRepository.GetAllAsync();

                try
                {
                    var freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);
                    var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                    if (freeBoard != null && freeBoard.Writer != loginedAccount.Nickname) // 작성자와 로그인한 사용자가 같지 않으면
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }

                    var boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                    // Encrypt an img tag file path
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(freeBoard?.Content ?? "");
                    var isImgTagIncluded = false;

                    foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                    {
                        isImgTagIncluded = true;
                        var filePath = imgTag.GetAttributeValue("alt", "");

                        // 파일을 다운로드하는 메서드
                        var fileData = await fileRepository.DownloadAsync(filePath);

                        if (fileData == null) continue;
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        var base64Data = Convert.ToBase64String(fileData);

                        // img 태그에 base64 데이터 저장 (src에 넣는 대신 data-file 속성 사용)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }

                    if (freeBoard != null)
                    {
                        freeBoard.Content = htmlDocument.DocumentNode.OuterHtml;

                        freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
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
                    }

                    if (string.IsNullOrEmpty(
                            freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                        return View(freeForumOutputViewModel);
                    {
                        var fileData = await fileRepository.DownloadAsync(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                        if (fileData == null) return View(freeForumOutputViewModel);
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(freeForumOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        if (freeForumOutputViewModel.BoardOutputViewModel == null)
                            return View(freeForumOutputViewModel);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        freeForumOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType =
                            contentType;
                    }

                    return View(freeForumOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("FreeForum", "Forum");
                }
            }
            // 목록 (list)
            default:
            {
                #region 게시판 기본 페이징 로직
                const int pageSize = 5;
                if (page < 1)
                {
                    page = 1;
                }
                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직
                var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id).ToList();
                #endregion

                #region 검색어 로직
                switch (searchType)
                {
                    case "Title" when !string.IsNullOrEmpty(searchText):
                    {
                        var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

                        if (isAccountSessionExist) // 로그인이 되어 있다면
                        {
                            var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            switch (loginedAccount.Role)
                            {
                                // 관리자는 모든 게시물을 볼 수 있음
                                case Role.Admin:
                                    freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                                    break;
                                // 사용자는 보통 글 + 자신이 쓴 비밀글만 볼 수 있음
                                case Role.User:
                                {
                                    List<Board> searchFreeBoards = [];
                                    var noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                                    var myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                                    searchFreeBoards.AddRange(noLockedFreeBoards);
                                    searchFreeBoards.AddRange(myLockedFreeBoards);

                                    freeBoards = searchFreeBoards;
                                    break;
                                }
                                // 로그인 시, Admin 또는 User 권한이 없다면 버그임
                            }
                        }
                        else // 로그인이 되지 않았다면
                        {
                            freeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked); // 비 로그인 시, 비밀글은 검색 되지 않도록 해야함
                        }

                        break;
                    }
                    case "Writer" when !string.IsNullOrEmpty(searchText):
                        freeBoards = freeBoards.Where(a => a.Writer.Contains(searchText));
                        break;
                }
                #endregion

                FreeForumOutputViewModel freeForumOutputViewModel = new();

                #region 공지사항
                freeForumOutputViewModel.NoticeBoards = (await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum))).Where(m => m.Deleted == false && m.Noticed).OrderByDescending(a => a.Id);
                #endregion

                #region 게시판 목록
                freeForumOutputViewModel.Method = "list";
                #endregion

                #region 게시판 페이징 로직

                var enumerable = freeBoards.ToList();
                freeForumOutputViewModel.Pager = new Pager(enumerable.Count, page, pageSize);
                #endregion

                #region 게시판 데이터 로직
                freeForumOutputViewModel.Boards = enumerable.Skip((page - 1) * pageSize).Take(freeForumOutputViewModel.Pager.PageSize).ToList();
                #endregion

                ViewBag.SelectedSearchType = searchType;
                ViewBag.TypedSearchText = searchText;
                return View(freeForumOutputViewModel);
            }
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
            var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            var tempFreeBoard = freeBoards.FirstOrDefault(a => a.Id == id);

            return tempFreeBoard == null
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : (IActionResult)Json(new { result = true, freeBoard = tempFreeBoard });
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
                boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                var sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;
                #endregion

                #region Decrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(boardInputViewModel.Content);

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                {
                    var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;
                #endregion

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                {
                    return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });
                }

                if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                {
                    if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip") // zip 확장자만 가능
                    {
                        return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                    {
                        return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }
                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var isAccountSessionExist = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

                    if (isAccountSessionExist)
                    {
                        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
                        freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                        var tempFreeBoard = freeBoards.FirstOrDefault(a => a.Id == boardInputViewModel.Id);

                        if (tempFreeBoard != null && tempFreeBoard.Writer != loginedAccount.Nickname)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        if (tempFreeBoard != null)
                        {
                            tempFreeBoard.Title = boardInputViewModel.Title;
                            tempFreeBoard.Content = boardInputViewModel.Content;
                            tempFreeBoard.Locked = boardInputViewModel.Locked;
                            tempFreeBoard.Updated = DateTime.UtcNow;

                            await boardRepository.UpdateBoardAsync(tempFreeBoard);
                        }

                        var boardAttachedFiles = await boardAttachedFileRepository?.GetAllAsync()! ?? [];
                        var previousBoardAttachedFile = boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                        #region 첨부 파일 저장

                        #region 기존 첨부 파일 O && 신 첨부 파일 O
                        if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                            {
                                var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                var boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                                var guid = Guid.NewGuid().ToString().ToUpper();
                                var boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                var filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                                _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                                previousBoardAttachedFile.Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                previousBoardAttachedFile.Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Path = $"upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}";

                                await boardAttachedFileRepository?.UpdateBoardAttachedFileAsync(previousBoardAttachedFile)!;
                                await transaction.CommitAsync();
                                return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                            }

                            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                        }
                        #endregion

                        #region 기존 첨부 파일 O && 신 첨부 파일 X

                        if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                        {
                            previousBoardAttachedFile.Size = 0;
                            previousBoardAttachedFile.Name = "";
                            previousBoardAttachedFile.Extension = "";
                            previousBoardAttachedFile.Path = "";

                            await boardAttachedFileRepository?.UpdateBoardAttachedFileAsync(previousBoardAttachedFile)!;
                            await transaction.CommitAsync();
                            return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                        }
                        #endregion

                        #region 기존 첨부 파일 X && 신 첨부 파일 O

                        if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                            {
                                var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                var boardAttachedFilePath = Path.Combine("upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");

                                var guid = Guid.NewGuid().ToString().ToUpper();
                                var boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                var filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

                                _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                                await boardAttachedFileRepository?.SaveBoardAttachedFileAsync(new BoardAttachedFile
                                {
                                    BoardId = createdBoardId,
                                    Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                    Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                    Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                    Path = $"upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}"
                                })!;
                                await transaction.CommitAsync();
                                return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                            }

                            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                        }
                        #endregion

                        #region 기존 첨부 파일 X && 신 첨부 파일 X

                        await transaction.CommitAsync();
                        return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                        #endregion

                        #endregion
                    }

                    return Json(new { result = false, error = localizer["Please Login to edit board"].Value });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel)
    {
        try
        {
            var freeBoards = await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.FreeForum));
            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            {
                var tempFreeBoard = freeBoards.FirstOrDefault(a => a.Id == boardInputViewModel.Id);

                if (tempFreeBoard == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);
                var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                if (loginedAccount.Nickname != tempFreeBoard.Writer && loginedAccount.Role != Role.Admin) // 방문한 사람이 작성자 또는 관리자인 경우
                    return Json(new
                        { result = true, message = localizer["You do not have permission to delete."].Value });

                #region 게시물 삭제
                tempFreeBoard.Deleted = true;
                await boardRepository.DeleteBoardAsync(tempFreeBoard);
                #endregion

                return Json(new { result = true, message = localizer["The board has been successfully deleted."].Value });

            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            var boardComment = await boardCommentRepository.GetBoardCommentAsync(id);

            if (boardComment == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            _ = HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (loginedAccount.Nickname != boardComment.Writer && loginedAccount.Role != Role.Admin) // 삭제하는 사람이 작성자 또는 관리자인 경우
                return Json(new
                    { result = true, message = localizer["You do not have permission to delete."].Value });

            #region 게시물 삭제
            boardComment.Deleted = true;
            await boardCommentRepository.DeleteBoardCommentAsync(boardComment);
            #endregion

            return Json(new { result = true });

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