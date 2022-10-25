using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Maroik.WebSite.Models.ViewModels.Forum;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        private readonly ILogger<ForumController> logger;
        private readonly IBoardRepository boardRepository;

        public ForumController(ILogger<ForumController> logger, IBoardRepository boardRepository)
        {
            this.logger = logger;
            this.boardRepository = boardRepository;
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
        #endregion
    }
}
