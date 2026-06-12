using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Maroik.WebSite.Controllers;

/// <summary>Handles application error and access-denied pages.</summary>
public class ExceptionController : BaseController
{
    private readonly ILogger<ExceptionController> _logger;

    /// <summary>Initializes a new instance of <see cref="ExceptionController"/> with the supplied dependencies.</summary>
    public ExceptionController(
        ILogger<ExceptionController> logger,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
        : base(accountService, dashboardService, serverSettings, sessionService)
    {
        _logger = logger;
    }

    /// <summary>Returns the access-denied view.</summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    /// <summary>Returns the generic error view.</summary>
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
