using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>Serves the file-drive index views for each role (Admin / User / Anonymous).</summary>
public class DriveController : BaseController
{
    private readonly IHtmlLocalizer<DriveController> _localizer;
    private readonly ILogger<DriveController> _logger;

    /// <summary>Initializes a new instance of <see cref="DriveController"/> with the supplied dependencies.</summary>
    public DriveController(
        IHtmlLocalizer<DriveController> localizer,
        ILogger<DriveController> logger,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
        : base(accountService, dashboardService, serverSettings, sessionService)
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