using Microsoft.AspNetCore.Mvc;

namespace Maroik.WebSite.Controllers;

public class DevelopController : Controller
{
    #region WebAPI

    #region Read
    [HttpGet]
    public IActionResult Api()
    {
        return View();
    }
    #endregion

    #endregion
}