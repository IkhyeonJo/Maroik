using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.WebSite.Controllers
{
    public class CalendarController : Controller
    {
        private readonly IHtmlLocalizer<CalendarController> localizer;
        private readonly ILogger<CalendarController> logger;

        public CalendarController(IHtmlLocalizer<CalendarController> localizer, ILogger<CalendarController> logger)
        {
            this.localizer = localizer;
            this.logger = logger;
        }

        #region Calendar

        #region Read
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        #endregion

        #endregion
    }
}