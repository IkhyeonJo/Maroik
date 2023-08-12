using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.WebSite.Controllers
{
    public class DevelopController : Controller
    {
        private readonly IHtmlLocalizer<DevelopController> localizer;
        private readonly ILogger<DevelopController> logger;

        public DevelopController(IHtmlLocalizer<DevelopController> localizer, ILogger<DevelopController> logger)
        {
            this.localizer = localizer;
            this.logger = logger;
        }

        #region WebAPI

        #region Read
        [HttpGet]
        public IActionResult API()
        {
            return View();
        }
        #endregion

        #endregion
    }
}