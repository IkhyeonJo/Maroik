using Ganss.Xss;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        private readonly ILogger<ForumController> logger;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IBoardRepository boardRepository;
        private readonly IAttachedFileRepository attachedFileRepository;
        private readonly IAccountRepository accountRepository;

        public ForumController(ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment, IAttachedFileRepository attachedFileRepository, IAccountRepository accountRepository)
        {
            this.logger = logger;
            this.boardRepository = boardRepository;
            this.hostEnvironment = hostEnvironment;
            this.attachedFileRepository = attachedFileRepository;
            this.accountRepository = accountRepository;
        }

        #region 자유게시판

        #region Create

        #region Write
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

                    if (!(boardInputViewModel.Title.Length > 0 && boardInputViewModel.Title.Length <= 30)) // 제목 길이
                    {
                        return Json(new { result = false, error = "Title length is must be between 1 and 30 characters" });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = "content length is must be between 0 and 16384 characters" });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(boardInputViewModel.UploadedFile.ContentType.ToUpper() == "application/x-zip-compressed".ToUpper())) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = "Only zip extension allowed." });
                        }

                        if (!(boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = "uploaded file size must be smaller than 10MB." });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                            var board = new Board()
                            {
                                Type = "Free", // 자유게시판
                                Title = boardInputViewModel.Title,
                                Content = boardInputViewModel.Content,
                                Writer = loginedAccount.Nickname,
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Views = 0,
                                Deleted = false
                            };
                            await boardRepository.WriteBoardAsync(board);

                            #region 첨부 파일 저장
                            if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                            {
                                if (boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    if (boardInputViewModel.UploadedFile.ContentType.ToUpper() == "application/x-zip-compressed".ToUpper())
                                    {
                                        var createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                                        string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                        if (!Directory.Exists(attachedFilePath))
                                            Directory.CreateDirectory(attachedFilePath);
                                        string guid = Guid.NewGuid().ToString().ToUpper();
                                        string attachedFile = $"{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
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
                                            Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}"
                                        });

                                    }
                                    else // 이미지 png 또는 jpg 외 확장자는 거절
                                    {
                                        return Ok(new { result = false, errorMessage = "Only .zip file allowed" });
                                    }
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = "File Size must be smaller than 10MB." });
                                }

                            }
                            #endregion

                            return Json(new { result = true, message = "The board has been successfully created." });
                        }
                        else
                        {
                            return Json(new { result = false, error = "Please Login to write board" });
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
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
                return Ok(new { result = false, errorMessage = "Please attach a file" });
            }
            else
            {
                if (summernoteImageFile.Length > 0 && summernoteImageFile.Length <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if (summernoteImageFile.ContentType.ToUpper() == "image/png".ToUpper() || summernoteImageFile.ContentType.ToUpper() == "image/jpeg".ToUpper())
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
                    else // 이미지 png 또는 jpg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = "Only .jpg or .png file allowed" });
                    }
                }
                else
                {
                    return Ok(new { result = false, errorMessage = "File Size must be smaller than 10MB." });
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
                freeForumOutputViewModel.EditBoardId = boardId ?? default;

                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var attachedFiles = await attachedFileRepository.GetAllAsync();

                try
                {
                    var freeBoard = freeBoards.FirstOrDefault(m => m.Id == boardId);
                    
                    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                    var loginedAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

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
                freeBoards = freeBoards.Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                #endregion

                #region 검색어 로직
                if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
                {
                    freeBoards = freeBoards.Where(a => a.Title.Contains(searchText));
                }
                else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
                {
                    freeBoards = freeBoards.Where(a => a.Writer.Contains(searchText));
                }
                #endregion

                var freeForumOutputViewModel = new FreeForumOutputViewModel();

                #region 게시판 목록
                freeForumOutputViewModel.Method = "list";
                #endregion

                #region 게시판 페이징 로직
                freeForumOutputViewModel.Pager = new Pager(freeBoards.Count(), page, pageSize);
                #endregion

                #region 게시판 데이터 로직
                freeForumOutputViewModel.Boards = freeBoards.Skip((page - 1) * pageSize).Take(freeForumOutputViewModel.Pager.PageSize).ToList();
                #endregion

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
                    return Json(new { result = false, error = "No freeBoard exists" });
                }
                else
                {
                    var tempFreeBoard = freeBoards.Where(a => a.Id == id).FirstOrDefault();

                    if (tempFreeBoard == null)
                    {
                        return Json(new { result = false, error = "Input is invalid" });
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

                    if (!(boardInputViewModel.Title.Length > 0 && boardInputViewModel.Title.Length <= 30)) // 제목 길이
                    {
                        return Json(new { result = false, error = "Title length is must be between 1 and 30 characters" });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = "content length is must be between 0 and 16384 characters" });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(boardInputViewModel.UploadedFile.ContentType.ToUpper() == "application/x-zip-compressed".ToUpper())) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = "Only zip extension allowed." });
                        }

                        if (!(boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = "uploaded file size must be smaller than 10MB." });
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
                                return Json(new { result = false, error = "Input is invalid" });
                            }

                            tempFreeBoard.Title = boardInputViewModel.Title;
                            tempFreeBoard.Content = boardInputViewModel.Content;
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
                                    if (boardInputViewModel.UploadedFile.ContentType.ToUpper() == "application/x-zip-compressed".ToUpper())
                                    {
                                        var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                        string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                        if (!Directory.Exists(attachedFilePath))
                                            Directory.CreateDirectory(attachedFilePath);
                                        string guid = Guid.NewGuid().ToString().ToUpper();
                                        string attachedFile = $"{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
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
                                        previousAttachedFile.Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";

                                        await attachedFileRepository.UpdateAttachedFileAsync(previousAttachedFile);

                                        return Json(new { result = true, message = "The board has been successfully updated." });
                                    }
                                    else // 이미지 png 또는 jpg 외 확장자는 거절
                                    {
                                        return Ok(new { result = false, errorMessage = "Only .zip file allowed" });
                                    }
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = "File Size must be smaller than 10MB." });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 O && 신 첨부 파일 X
                            else if (previousAttachedFile != null && boardInputViewModel.UploadedFile == null)
                            {
                                return Json(new { result = true, message = "The board has been successfully updated." });
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 O
                            else if (previousAttachedFile == null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length > 0 && boardInputViewModel.UploadedFile.Length <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    if (boardInputViewModel.UploadedFile.ContentType.ToUpper() == "application/x-zip-compressed".ToUpper())
                                    {
                                        var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                        string attachedFilePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "attachedFiles", $"{createdBoardId}");
                                        if (!Directory.Exists(attachedFilePath))
                                            Directory.CreateDirectory(attachedFilePath);
                                        string guid = Guid.NewGuid().ToString().ToUpper();
                                        string attachedFile = $"{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
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
                                            Path = $"/upload/Forum/attachedFiles/{createdBoardId}/{Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName)}-{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}"
                                        });

                                        return Json(new { result = true, message = "The board has been successfully updated." });
                                    }
                                    else // 이미지 png 또는 jpg 외 확장자는 거절
                                    {
                                        return Ok(new { result = false, errorMessage = "Only .zip file allowed" });
                                    }
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = "File Size must be smaller than 10MB." });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 X
                            else
                            {
                                return Json(new { result = true, message = "The board has been successfully updated." });
                            }
                            #endregion

                            #endregion



                        }
                        else
                        {
                            return Json(new { result = false, error = "Please Login to edit board" });
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
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
                    return Json(new { result = false, error = "Input is invalid" });
                }
                else
                {
                    var tempFreeBoard = freeBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                    if (tempFreeBoard == null)
                    {
                        return Json(new { result = false, error = "Input is invalid" });
                    }
                    else
                    {
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                        var loginedAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                        if ((loginedAccount.Nickname == tempFreeBoard.Writer) || loginedAccount.Role == Role.Admin) // 방문한 사람이 작성자 또는 관리자인 경우
                        {
                            #region 게시물 삭제
                            tempFreeBoard.Deleted = true;
                            await boardRepository.DeleteBoardAsync(tempFreeBoard);
                            #endregion

                            return Json(new { result = true, message = "The board has been successfully deleted." });
                        }
                        else
                        {
                            return Json(new { result = true, message = "You do not have permission to delete." });
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

        #endregion

    }
}
