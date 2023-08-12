using Ganss.Xss;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        private readonly IHtmlLocalizer<ForumController> localizer;
        private readonly ILogger<ForumController> logger;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IBoardRepository boardRepository;
        private readonly IAttachedFileRepository attachedFileRepository;
        private readonly IAccountRepository accountRepository;
        private readonly ICommentRepository commentRepository;

        public ForumController(IHtmlLocalizer<ForumController> localizer, ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment, IAttachedFileRepository attachedFileRepository, IAccountRepository accountRepository, ICommentRepository commentRepository)
        {
            this.localizer = localizer;
            this.logger = logger;
            this.boardRepository = boardRepository;
            this.hostEnvironment = hostEnvironment;
            this.attachedFileRepository = attachedFileRepository;
            this.accountRepository = accountRepository;
            this.commentRepository = commentRepository;
        }

        #region 자유게시판

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
                    boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

                    #region HtmlSanitizer
                    var sanitizer = new HtmlSanitizer();
                    sanitizer.AllowedAttributes.Add("class");
                    var sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                    boardInputViewModel.Content = sanitized;
                    #endregion

                    if (!(boardInputViewModel.Title.Length > 0 && boardInputViewModel.Title.Length <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                        }

                        if (!(boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                            var board = new Board();

                            if (loginedAccount.Role == Role.Admin) // 관리자인 경우
                            {
                                board.Type = "Free"; // 자유게시판
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.Views = 0;
                                board.Deleted = false;
                                board.Locked = boardInputViewModel.Locked;
                                board.Noticed = boardInputViewModel.Noticed;
                            }
                            else if (loginedAccount.Role == Role.User) // 일반 사용자인 경우
                            {
                                board.Type = "Free"; // 자유게시판
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.Views = 0;
                                board.Deleted = false;
                                board.Locked = boardInputViewModel.Locked;
                            }
                            else // 비로그인 사용자인 경우
                            {
                                return Json(new { result = false, error = localizer["Please Login to write board"].Value });
                            }

                            await boardRepository.WriteBoardAsync(board);

                            #region 첨부 파일 저장
                            if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                            {
                                if (boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    var createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(attachedFilePath))
                                        Directory.CreateDirectory(attachedFilePath);
                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(attachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await attachedFileRepository.SaveAttachedFileAsync(new AttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{attachedFile}"
                                    });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                                }

                            }
                            #endregion

                            return Json(new { result = true, message = localizer["The board has been successfully created."].Value });
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Please Login to write board"].Value });
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region FreeComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> WriteFreeComment([FromBody] CommentInputViewModel commentInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(commentInputViewModel.Content))
                    {
                        return Json(new { result = false, error = localizer["Please enter a comment."].Value });
                    }

                    #region HtmlSanitizer
                    var sanitizer = new HtmlSanitizer();
                    sanitizer.AllowedAttributes.Add("class");
                    var sanitized = sanitizer.Sanitize(commentInputViewModel.Content);
                    commentInputViewModel.Content = sanitized;
                    #endregion

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            var totalCommentsInSelectedBoard = await commentRepository.GetOneBoardComments(commentInputViewModel.BoardId);

                            var comment = new Comment()
                            {
                                BoardId = commentInputViewModel.BoardId,
                                Order = totalCommentsInSelectedBoard?.Count() ?? default,
                                AvatarImagePath = loginedAccount.AvatarImagePath,
                                Writer = loginedAccount.Nickname,
                                Content = commentInputViewModel.Content,
                                Created = DateTime.UtcNow,
                                Deleted = false
                            };
                            await commentRepository.WriteCommentAsync(comment);

                            return Json(new { result = true, boardId = commentInputViewModel.BoardId, page = commentInputViewModel.DetailCurrentPage });
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Please Login to write comment."].Value });
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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
            if (summernoteImageFile == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });
            }
            else
            {
                if (summernoteImageFile.Length > 0 && summernoteImageFile.Length <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if ((Path.GetExtension(summernoteImageFile.FileName) == ".png") || (Path.GetExtension(summernoteImageFile.FileName) == ".jpg") || (Path.GetExtension(summernoteImageFile.FileName) == ".jpeg"))
                    {

                        string summernoteImagePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "summernote", "images");
                        if (!Directory.Exists(summernoteImagePath))
                            Directory.CreateDirectory(summernoteImagePath);
                        string imageFile = $"{Guid.NewGuid().ToString().ToUpper()}{Path.GetExtension(summernoteImageFile.FileName)}";
                        string filePath = Path.Combine(summernoteImagePath, imageFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await summernoteImageFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }
                        return Ok(new { result = true, filePath = $"/upload/Forum/summernote/images/{imageFile}" });
                    }
                    else // 이미지 png 또는 jpg 또는 jpeg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = localizer["Only .jpg or jpeg or .png file allowed."].Value });
                    }
                }
                else
                {
                    return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                }
            }
        }
        #endregion

        #endregion

        #region Read

        #region Edit, List, Detail, Write
        public async Task<IActionResult> FreeForum(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "")
        {
            if (method == "write") // 글쓰기 (write)
            {
                var freeForumOutputViewModel = new FreeForumOutputViewModel();

                #region 게시판 글쓰기
                freeForumOutputViewModel.Method = method;
                #endregion

                bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                if (isAccountSessionExist) // Role: Admin or User
                {
                    return View(freeForumOutputViewModel);
                }
                else // Role: Anonymous
                {
                    return RedirectToAction("Login", "Account");
                }
            }
            else if (method == "detail") // 상세보기 (detail)
            {
                if (boardId == null)
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

                var freeForumOutputViewModel = new FreeForumOutputViewModel();

                freeForumOutputViewModel.Method = method;
                freeForumOutputViewModel.DetailCurrentPage = page;
                freeForumOutputViewModel.DetailBoardId = boardId ?? default;

                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var attachedFiles = await attachedFileRepository.GetAllAsync();

                try
                {
                    var freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    if (freeBoard.Locked)
                    {
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte); // 작성자 또는 관리자인 경우
                        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                        {
                            return RedirectToAction("FreeForum", "Forum");
                        }
                    }

                    freeBoard.Views++;
                    await boardRepository.UpdateBoardAsync(freeBoard);
                    var attachedFile = attachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.Views,
                        Content = freeBoard.Content,
                        Updated = freeBoard.Updated,
                        AttachedFileName = attachedFile?.Name ?? "",
                        AttachedFileExtension = attachedFile?.Extension ?? "",
                        AttachedFileSize = attachedFile?.Size ?? 0,
                        AttachedFilePath = attachedFile?.Path ?? ""
                    };

                    return View(freeForumOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("FreeForum", "Forum");
                }
            }
            else if (method == "edit") // 수정 (edit)
            {
                if (boardId == null)
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

                var freeForumOutputViewModel = new FreeForumOutputViewModel();

                freeForumOutputViewModel.Method = method;
                freeForumOutputViewModel.EditCurrentPage = page;
                freeForumOutputViewModel.EditBoardId = boardId ?? default;

                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var attachedFiles = await attachedFileRepository.GetAllAsync();

                try
                {
                    var freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                    var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if (freeBoard.Writer != loginedAccount.Nickname) // 작성자와 로그인한 사용자가 같지 않으면
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }

                    var attachedFile = attachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.Views,
                        Content = freeBoard.Content,
                        Locked = freeBoard.Locked,
                        Updated = freeBoard.Updated,
                        AttachedFileName = attachedFile?.Name ?? "",
                        AttachedFileExtension = attachedFile?.Extension ?? "",
                        AttachedFileSize = attachedFile?.Size ?? 0,
                        AttachedFilePath = attachedFile?.Path ?? ""
                    };

                    return View(freeForumOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("FreeForum", "Forum");
                }

            }
            else // 목록 (list)
            {
                #region 게시판 기본 페이징 로직
                const int pageSize = 5;
                if (page < 1)
                    page = 1;
                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직
                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
                #endregion

                #region 검색어 로직
                if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                    if (isAccountSessionExist) // 로그인이 되어 있다면
                    {
                        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        if (loginedAccount.Role == Role.Admin) // 관리자는 모든 게시물을 볼 수 있음
                        {
                            freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                        }
                        else if (loginedAccount.Role == Role.User) // 사용자는 보통 글 + 자신이 쓴 비밀글만 볼 수 있음
                        {
                            var searchFreeBoards = new List<Board>();
                            var noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                            var myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                            searchFreeBoards.AddRange(noLockedFreeBoards);
                            searchFreeBoards.AddRange(myLockedFreeBoards);

                            freeBoards = searchFreeBoards;
                        }
                        else // 로그인 시, Admin 또는 User 권한이 없다면 버그임
                        {

                        }
                    }
                    else // 로그인이 되지 않았다면
                    {
                        freeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked); // 비 로그인 시, 비밀글은 검색 되지 않도록 해야함
                    }
                }
                else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
                {
                    freeBoards = freeBoards.Where(a => a.Writer.Contains(searchText));
                }
                #endregion

                var freeForumOutputViewModel = new FreeForumOutputViewModel();

                #region 공지사항
                freeForumOutputViewModel.NoticeBoards = (await boardRepository.GetOneTypeBoards("Free")).Where(m => m.Deleted == false && m.Noticed == true).OrderByDescending(a => a.Id);
                #endregion

                #region 게시판 목록
                freeForumOutputViewModel.Method = "list";
                #endregion

                #region 게시판 페이징 로직
                freeForumOutputViewModel.Pager = new Pager(freeBoards.Count(), page, pageSize);
                #endregion

                #region 게시판 데이터 로직
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
                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                if (freeBoards == null)
                {
                    return Json(new { result = false, error = localizer["No freeBoard exists"].Value });
                }
                else
                {
                    var tempFreeBoard = freeBoards.Where(a => a.Id == id).FirstOrDefault();

                    if (tempFreeBoard == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        return Json(new { result = true, freeBoard = tempFreeBoard });
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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
                    var sanitizer = new HtmlSanitizer();
                    sanitizer.AllowedAttributes.Add("class");
                    var sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                    boardInputViewModel.Content = sanitized;
                    #endregion

                    if (!(boardInputViewModel.Title.Length > 0 && boardInputViewModel.Title.Length <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                        }

                        if (!(boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                            var tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                            if (tempFreeBoard.Writer != loginedAccount.Nickname)
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }

                            tempFreeBoard.Title = boardInputViewModel.Title;
                            tempFreeBoard.Content = boardInputViewModel.Content;
                            tempFreeBoard.Locked = boardInputViewModel.Locked;
                            tempFreeBoard.Updated = DateTime.UtcNow;

                            await boardRepository.UpdateBoardAsync(tempFreeBoard);

                            var attachedFiles = await attachedFileRepository?.GetAllAsync();
                            var previousAttachedFile = attachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                            #region 첨부 파일 저장

                            #region 기존 첨부 파일 O && 신 첨부 파일 O
                            if (previousAttachedFile != null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(attachedFilePath))
                                        Directory.CreateDirectory(attachedFilePath);
                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(attachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    previousAttachedFile.Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                    previousAttachedFile.Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousAttachedFile.Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousAttachedFile.Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{attachedFile}";

                                    await attachedFileRepository.UpdateAttachedFileAsync(previousAttachedFile);

                                    return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 O && 신 첨부 파일 X
                            else if (previousAttachedFile != null && boardInputViewModel.UploadedFile == null)
                            {
                                previousAttachedFile.Size = 0;
                                previousAttachedFile.Name = "";
                                previousAttachedFile.Extension = "";
                                previousAttachedFile.Path = "";

                                await attachedFileRepository.UpdateAttachedFileAsync(previousAttachedFile);

                                return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 O
                            else if (previousAttachedFile == null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(attachedFilePath))
                                        Directory.CreateDirectory(attachedFilePath);
                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(attachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await attachedFileRepository.SaveAttachedFileAsync(new AttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{attachedFile}"
                                    });

                                    return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 X
                            else
                            {
                                return Json(new { result = true, message = localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #endregion
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Please Login to edit board"].Value });
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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
                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                if (freeBoards == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    var tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                    if (tempFreeBoard == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                        var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                        if ((loginedAccount.Nickname == tempFreeBoard.Writer) || loginedAccount.Role == Role.Admin) // 방문한 사람이 작성자 또는 관리자인 경우
                        {
                            #region 게시물 삭제
                            tempFreeBoard.Deleted = true;
                            await boardRepository.DeleteBoardAsync(tempFreeBoard);
                            #endregion

                            return Json(new { result = true, message = localizer["The board has been successfully deleted."].Value });
                        }
                        else
                        {
                            return Json(new { result = true, message = localizer["You do not have permission to delete."].Value });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Comment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteComment(int id)
        {
            try
            {
                var comment = await commentRepository.GetComment(id);

                if (comment == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                    var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if ((loginedAccount.Nickname == comment.Writer) || loginedAccount.Role == Role.Admin) // 삭제하는 사람이 작성자 또는 관리자인 경우
                    {
                        #region 게시물 삭제
                        comment.Deleted = true;
                        await commentRepository.DeleteCommentAsync(comment);
                        #endregion

                        return Json(new { result = true });
                    }
                    else
                    {
                        return Json(new { result = true, message = localizer["You do not have permission to delete."].Value });
                    }

                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #endregion

        #endregion

    }
}
