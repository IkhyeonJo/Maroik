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

        public async Task<IActionResult> FreeForum(int page=1)
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

            #region 게시판 페이징 로직
            freeForumOutputViewModel.Pager = new Pager(freeBoards.Count(), page, pageSize);
            #endregion

            #region 게시판 데이터 로직
            freeForumOutputViewModel.Boards = freeBoards.Skip((page - 1) * pageSize).Take(freeForumOutputViewModel.Pager.PageSize).ToList();
            #endregion

            return View(freeForumOutputViewModel);
        }
    }
}
