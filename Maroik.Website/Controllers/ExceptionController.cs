using System.Diagnostics;
using Maroik.Website.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.Website.Controllers;

/// <summary>Handles application error and access-denied pages.</summary>
public class ExceptionController : Controller
{
    /// <summary>Initializes a new instance of <see cref="ExceptionController"/> with the supplied dependencies.</summary>
    public ExceptionController()
    {
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
