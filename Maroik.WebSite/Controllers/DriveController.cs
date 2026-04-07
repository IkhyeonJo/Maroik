using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

public class DriveController : BaseController
{
    private readonly IHtmlLocalizer<DriveController> _localizer;
    private readonly ILogger<DriveController> _logger;

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