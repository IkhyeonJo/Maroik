using Microsoft.AspNetCore.Mvc;

namespace Maroik.Website.Controllers;

/// <summary>Serves the file-drive index views for each role (Admin / User / Anonymous).</summary>
public class DriveController : Controller
{
    /// <summary>Initializes a new instance of <see cref="DriveController"/> with the supplied dependencies.</summary>
    public DriveController()
    {
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
