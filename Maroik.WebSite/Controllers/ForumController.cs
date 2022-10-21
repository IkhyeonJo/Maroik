using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
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

        public async Task<IActionResult> Free()
        {
            var freeBoards = await boardRepository.GetOneTypeBoards("Free");
            
            var freeOutputViewModel = new FreeOutputViewModel();
            freeOutputViewModel.FreeBoards = freeBoards;
            return View(freeOutputViewModel);
        }
    }
}
