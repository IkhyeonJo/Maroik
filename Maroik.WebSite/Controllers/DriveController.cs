using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.WebSite.Controllers;

/// <summary>Serves the file-drive index views for each role (Admin / User / Anonymous).</summary>
public class DriveController : Controller
{
    private readonly IHtmlLocalizer<DriveController> _localizer;
    private readonly ILogger<DriveController> _logger;

    /// <summary>Initializes a new instance of <see cref="DriveController"/> with the supplied dependencies.</summary>
    public DriveController(
        IHtmlLocalizer<DriveController> localizer,
        ILogger<DriveController> logger)
    {
        _localizer = localizer;
        _logger = logger;
    }

    #region Drive

    #region Read
    /// <summary>Returns the admin drive view.</summary>
    [HttpGet]
    public IActionResult AdminIndex()
    {
        return View();
    }

    /// <summary>Returns the user drive view.</summary>
    [HttpGet]
    public IActionResult UserIndex()
    {
        return View();
    }

    /// <summary>Returns the anonymous drive view.</summary>
    [HttpGet]
    public IActionResult AnonymousIndex()
    {
        return View();
    }
    #endregion

    #endregion
}
