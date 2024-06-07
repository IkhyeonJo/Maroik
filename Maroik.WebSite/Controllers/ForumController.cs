using Ganss.Xss;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Forum;
using Maroik.WebSite.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        private readonly IHtmlLocalizer<ForumController> _localizer;
        private readonly ILogger<ForumController> _logger;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IBoardCommentRepository _boardCommentRepository;

        public ForumController(IHtmlLocalizer<ForumController> localizer, ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment, IBoardAttachedFileRepository boardAttachedFileRepository, IAccountRepository accountRepository, IBoardCommentRepository boardCommentRepository)
        {
            _localizer = localizer;
            _logger = logger;
            _boardRepository = boardRepository;
            _hostEnvironment = hostEnvironment;
            _boardAttachedFileRepository = boardAttachedFileRepository;
            _accountRepository = accountRepository;
            _boardCommentRepository = boardCommentRepository;
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
                    HtmlSanitizer sanitizer = new();
                    _ = sanitizer.AllowedAttributes.Add("class");
                    string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                    boardInputViewModel.Content = sanitized;
                    #endregion

                    if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                        }

                        if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                            Board board = new();

                            if (loginedAccount.Role == Role.Admin) // 관리자인 경우
                            {
                                board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // 자유게시판
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
                                board.Type = EnumHelper.GetDescription(BoardType.FreeForum); // 자유게시판
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
                                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
                            }

                            await _boardRepository.WriteBoardAsync(board);

                            #region 첨부 파일 저장
                            if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}"
                                    });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }

                            }
                            #endregion

                            return Json(new { result = true, message = _localizer["The board has been successfully created."].Value });
                        }
                        else
                        {
                            return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
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
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            IEnumerable<Board> freeForumBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
                            freeForumBoards = freeForumBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                            Board freeForumBoard = freeForumBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                            if (freeForumBoard.Id != boardCommentInputViewModel.BoardId)
                            {
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            IEnumerable<BoardComment> totalBoardCommentsInSelectedBoard = await _boardCommentRepository.GetOneBoardComments(boardCommentInputViewModel.BoardId);

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
            if (summernoteImageFile == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
            }
            else
            {
                if (summernoteImageFile.Length is > 0 and <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if (Path.GetExtension(summernoteImageFile.FileName) is ".png" or ".jpg" or ".jpeg")
                    {

                        string summernoteImagePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "summernote", "images");
                        if (!Directory.Exists(summernoteImagePath))
                        {
                            _ = Directory.CreateDirectory(summernoteImagePath);
                        }

                        string imageFile = $"{Guid.NewGuid().ToString().ToUpper()}{Path.GetExtension(summernoteImageFile.FileName)}";
                        string filePath = Path.Combine(summernoteImagePath, imageFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await summernoteImageFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }
                        return Ok(new { result = true, filePath = $"/upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/summernote/images/{imageFile}" });
                    }
                    else // 이미지 png 또는 jpg 또는 jpeg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });
                    }
                }
                else
                {
                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
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
                FreeForumOutputViewModel freeForumOutputViewModel = new();

                #region 게시판 글쓰기
                freeForumOutputViewModel.Method = method;
                #endregion

                bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                return isAccountSessionExist ? View(freeForumOutputViewModel) : RedirectToAction("Login", "Account");
            }
            else if (method == "detail") // 상세보기 (detail)
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

                IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

                try
                {
                    Board freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    if (freeBoard.Locked)
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte); // 작성자 또는 관리자인 경우
                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        if (!(loginedAccount.Nickname == freeBoard.Writer || loginedAccount.Role == Role.Admin))
                        {
                            return RedirectToAction("FreeForum", "Forum");
                        }
                    }

                    freeBoard.Views++;
                    await _boardRepository.UpdateBoardAsync(freeBoard);
                    BoardAttachedFile boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.Views,
                        Content = freeBoard.Content,
                        Updated = freeBoard.Updated,
                        BoardAttachedFileName = boardAttachedFile?.Name ?? "",
                        BoardAttachedFileExtension = boardAttachedFile?.Extension ?? "",
                        BoardAttachedFileSize = boardAttachedFile?.Size ?? 0,
                        BoardAttachedFilePath = boardAttachedFile?.Path ?? ""
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

                FreeForumOutputViewModel freeForumOutputViewModel = new()
                {
                    Method = method,
                    EditCurrentPage = page,
                    EditBoardId = boardId ?? default
                };

                IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

                try
                {
                    Board freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);

                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                    Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if (freeBoard.Writer != loginedAccount.Nickname) // 작성자와 로그인한 사용자가 같지 않으면
                    {
                        return RedirectToAction("FreeForum", "Forum");
                    }

                    BoardAttachedFile boardAttachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    freeForumOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = freeBoard.Title,
                        Writer = freeBoard.Writer,
                        Views = freeBoard.Views,
                        Content = freeBoard.Content,
                        Locked = freeBoard.Locked,
                        Updated = freeBoard.Updated,
                        BoardAttachedFileName = boardAttachedFile?.Name ?? "",
                        BoardAttachedFileExtension = boardAttachedFile?.Extension ?? "",
                        BoardAttachedFileSize = boardAttachedFile?.Size ?? 0,
                        BoardAttachedFilePath = boardAttachedFile?.Path ?? ""
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
                {
                    page = 1;
                }
                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직
                IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
                freeBoards = freeBoards.Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
                #endregion

                #region 검색어 로직
                if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist) // 로그인이 되어 있다면
                    {
                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                        if (loginedAccount.Role == Role.Admin) // 관리자는 모든 게시물을 볼 수 있음
                        {
                            freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                        }
                        else if (loginedAccount.Role == Role.User) // 사용자는 보통 글 + 자신이 쓴 비밀글만 볼 수 있음
                        {
                            List<Board> searchFreeBoards = [];
                            List<Board> noLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                            List<Board> myLockedFreeBoards = freeBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
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

                FreeForumOutputViewModel freeForumOutputViewModel = new();

                #region 공지사항
                freeForumOutputViewModel.NoticeBoards = (await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum))).Where(m => m.Deleted == false && m.Noticed == true).OrderByDescending(a => a.Id);
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
                IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
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
                    boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

                    #region HtmlSanitizer
                    HtmlSanitizer sanitizer = new();
                    _ = sanitizer.AllowedAttributes.Add("class");
                    string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                    boardInputViewModel.Content = sanitized;
                    #endregion

                    if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                        }

                        if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
                            freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                            Board tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                            if (tempFreeBoard.Writer != loginedAccount.Nickname)
                            {
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            tempFreeBoard.Title = boardInputViewModel.Title;
                            tempFreeBoard.Content = boardInputViewModel.Content;
                            tempFreeBoard.Locked = boardInputViewModel.Locked;
                            tempFreeBoard.Updated = DateTime.UtcNow;

                            await _boardRepository.UpdateBoardAsync(tempFreeBoard);

                            List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository?.GetAllAsync();
                            BoardAttachedFile previousBoardAttachedFile = boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                            #region 첨부 파일 저장

                            #region 기존 첨부 파일 O && 신 첨부 파일 O
                            if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    previousBoardAttachedFile.Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                    previousBoardAttachedFile.Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousBoardAttachedFile.Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousBoardAttachedFile.Path = $"/upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}";

                                    await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);

                                    return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 O && 신 첨부 파일 X
                            else if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                            {
                                previousBoardAttachedFile.Size = 0;
                                previousBoardAttachedFile.Name = "";
                                previousBoardAttachedFile.Extension = "";
                                previousBoardAttachedFile.Path = "";

                                await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);

                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 O
                            else if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", EnumHelper.GetDescription(BoardType.FreeForum), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string boardAttachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, boardAttachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Forum/{EnumHelper.GetDescription(BoardType.FreeForum)}/boardAttachedFiles/{createdBoardId}/{boardAttachedFile}"
                                    });

                                    return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 X
                            else
                            {
                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #endregion
                        }
                        else
                        {
                            return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });
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
                IEnumerable<Board> freeBoards = await _boardRepository.GetOneTypeBoards(EnumHelper.GetDescription(BoardType.FreeForum));
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
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                        Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                        if ((loginedAccount.Nickname == tempFreeBoard.Writer) || loginedAccount.Role == Role.Admin) // 방문한 사람이 작성자 또는 관리자인 경우
                        {
                            #region 게시물 삭제
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
                BoardComment boardComment = await _boardCommentRepository.GetBoardComment(id);

                if (boardComment == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                    Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if ((loginedAccount.Nickname == boardComment.Writer) || loginedAccount.Role == Role.Admin) // 삭제하는 사람이 작성자 또는 관리자인 경우
                    {
                        #region 게시물 삭제
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
}
