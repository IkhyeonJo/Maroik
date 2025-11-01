using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.WebSite.Controllers;

public class DriveController : Controller
{
    private readonly IHtmlLocalizer<DriveController> _localizer;
    private readonly ILogger<DriveController> _logger;

    public DriveController(IHtmlLocalizer<DriveController> localizer, ILogger<DriveController> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    #region Drive

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