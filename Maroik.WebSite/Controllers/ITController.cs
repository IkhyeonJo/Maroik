using Microsoft.AspNetCore.Mvc;

namespace Maroik.WebSite.Controllers
{
    public class ITController : Controller
    {
        public IActionResult CSharp()
        {
            return View();
        }
    }
}
