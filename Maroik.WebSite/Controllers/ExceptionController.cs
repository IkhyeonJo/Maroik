using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Maroik.WebSite.Controllers;

public class ExceptionController : BaseController
{
    private readonly ILogger<ExceptionController> _logger;

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

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
