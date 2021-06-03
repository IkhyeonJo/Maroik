using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Site.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using AspNetCoreMVCSample.src.Site.Models;

namespace Site.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private string message;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            this.logger = logger;
            message = $"Docker - ({config["HOSTNAME"]})";
        }

        public IActionResult Index()
        {
            ViewBag.Message = message;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
