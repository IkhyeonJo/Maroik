using Microsoft.AspNetCore.Mvc;

namespace Maroik.WebSite.Controllers
{
    public class ForumController : Controller
    {
        public IActionResult Free()
        {
            return View();
        }
    }
}
