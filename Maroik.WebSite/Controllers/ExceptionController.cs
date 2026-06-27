using Maroik.WebSite.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Maroik.WebSite.Controllers;

/// <summary>Handles application error and access-denied pages.</summary>
public class ExceptionController : Controller
{
    private readonly ILogger<ExceptionController> _logger;

    /// <summary>Initializes a new instance of <see cref="ExceptionController"/> with the supplied dependencies.</summary>
    public ExceptionController(ILogger<ExceptionController> logger)
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
