using Microsoft.AspNetCore.Mvc;

namespace Maroik.WebSite.Controllers
{
    public class FileController : Controller
    {
        #region File

        #region Read
        [HttpGet]
        public IActionResult AdminIndex()
        {
            return View();
        }

        [HttpGet]
        public IActionResult UserIndex()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AnonymousIndex()
        {
            return View();
        }
        #endregion

        #endregion
    }
}