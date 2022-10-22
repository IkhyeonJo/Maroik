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

        public async Task<IActionResult> FreeForum(int pg=1)
        {
            var freeForumOutputViewModel = new FreeForumOutputViewModel();
            freeForumOutputViewModel.Boards = await boardRepository.GetOneTypeBoards("Free");

            const int pageSize = 5;
            if (pg < 1)
                pg = 1;

            int recsCount = freeForumOutputViewModel.Boards.Count();

            var pager = new Pager(recsCount, pg, pageSize);

            int recSkip = (pg - 1) * pageSize;

            var data = freeForumOutputViewModel.Boards.Skip(recSkip).Take(pager.PageSize).ToList();

            ViewBag.Pager = pager;



            return View(data);
        }
    }
}
