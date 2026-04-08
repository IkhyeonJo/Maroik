using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>Serves the file-drive index views for each role (Admin / User / Anonymous).</summary>
public class DriveController(
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    #region Drive

    #region Read

    /// <summary>Returns the admin drive view.</summary>
    [HttpGet]
    public IActionResult AdminIndex() => View();

    /// <summary>Returns the user drive view.</summary>
    [HttpGet]
    public IActionResult UserIndex() => View();

    /// <summary>Returns the anonymous drive view.</summary>
    [HttpGet]
    public IActionResult AnonymousIndex() => View();

    #endregion

    #endregion
}
