using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Reflection;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        private readonly ILogger<ForumController> logger;
        private readonly IHostEnvironment hostEnvironment;
        private readonly IBoardRepository boardRepository;

        public ForumController(ILogger<ForumController> logger, IBoardRepository boardRepository, IHostEnvironment hostEnvironment)
        {
            this.logger = logger;
            this.boardRepository = boardRepository;
            this.hostEnvironment = hostEnvironment;
        }

        #region 자유게시판
        public async Task<IActionResult> FreeForum(string method = "list", int page = 1)
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
            else // 목록 (list)
            {
                #region 게시판 기본 페이징 로직
                const int pageSize = 5;
                if (page < 1)
                    page = 1;
                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직
                var freeBoards = await boardRepository.GetOneTypeBoards("Free");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
        {
            if(summernoteImageFile == null) // 파일 없을 시
            {
                return Ok( new { result = false, errorMessage = "Please attach a file" });
            }
            else
            {
                if (summernoteImageFile.Length > 0 && summernoteImageFile.Length <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if (summernoteImageFile.ContentType.ToUpper() == "image/png".ToUpper() || summernoteImageFile.ContentType.ToUpper() == "image/jpeg".ToUpper())
                    {

                        string summernoteImagePath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Forum", "FreeForum", "summernote", "images");
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
                        return Ok(new { result = true, filePath = $"/upload/Forum/FreeForum/summernote/images/{imageFile}" });
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
    }
}
