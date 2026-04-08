using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Maroik.WebSite.Controllers;

/// <summary>Handles application error and access-denied pages.</summary>
public class ExceptionController(
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    /// <summary>Returns the access-denied view.</summary>
    [HttpGet]
    public IActionResult AccessDenied() => View();

    /// <summary>Returns the generic error view.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
