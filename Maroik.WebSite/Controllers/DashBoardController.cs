using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Attributes;
using Maroik.WebSite.Mappings;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Serves the dashboard index views (Admin / User / Anonymous) and handles
/// language-culture changes and the user's default monetary-unit preference.
/// </summary>
public class DashBoardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IProfileService _profileService;
    private readonly ISessionService _sessionService;

    private const string HostResourceFilePath = "/app/Maroik.Log/HostResourceInfo.txt";
    private const string DockerResourceFilePath = "/app/Maroik.Log/DockerResourceInfo.txt";

    /// <summary>Initializes a new instance of <see cref="DashBoardController"/> with the required dependencies.</summary>
    public DashBoardController(
        IDashboardService dashboardService,
        IProfileService profileService,
        ISessionService sessionService)
    {
        _dashboardService = dashboardService;
        _profileService = profileService;
        _sessionService = sessionService;
    }

    #region Language change
    /// <summary>Sets the UI culture cookie to the requested locale.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public IActionResult CultureManagement([FromBody] LoginInputViewModel loginInputViewModel)
    {
        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(loginInputViewModel.Culture!)),
            new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict });

        return Json(new { result = true });
    }
    #endregion

    #region Admin
    /// <summary>Returns the admin dashboard view.</summary>
    [HttpGet]
    public IActionResult AdminIndex()
    {
        var resource = _dashboardService.GetServerResourceSummary(HostResourceFilePath, DockerResourceFilePath);

        ViewBag.HostCpuNumeric = resource.HostCpuNumeric;
        ViewBag.HostCpuInfo = resource.HostCpuInfo;
        ViewBag.MemUsageLimit = resource.MemUsageLimit;
        ViewBag.HostMemoryInfo = resource.HostMemoryInfo;
        ViewBag.DiskUsageLimit = resource.DiskUsageLimit;
        ViewBag.HostDiskInfo = resource.HostDiskInfo;
        ViewBag.DockerContainerResult = resource.DockerContainerResult;

        return View();
    }
    #endregion

    #region User

    #region Read
    /// <summary>Returns the user dashboard view populated with income/expenditure summary data.</summary>
    [HttpGet]
    public async Task<IActionResult> UserIndex(string year, string month, CancellationToken ct)
    {
        var loginedAccount = _sessionService.GetAccount();
        string? email      = loginedAccount?.Email;
        string? timeZoneId = loginedAccount?.TimeZoneIanaId;

        if (email == "demo@maroik.com") { year = "2025"; month = "06"; }

        var summary = await _dashboardService.GetSummaryAsync(email!, year, month, timeZoneId!, ct);
        return View(summary.ToViewModel(timeZoneId!));
    }
    #endregion

    #region Update
    /// <summary>Updates the logged-in user's default monetary unit preference.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UserUpdateDefaultMonetary([FromBody] UserIndexInputViewModel userIndexInputViewModel, CancellationToken ct)
    {
        try
        {
            string email = _sessionService.GetAccount()?.Email ?? "";
            await _profileService.UpdateDefaultMonetaryUnitAsync(email, userIndexInputViewModel.DefaultMonetaryUnit, ct);
            return Json(new { result = true });
        }
        catch
        {
            return Json(new { result = false });
        }
    }
    #endregion

    #endregion

    #region Anonymous
    /// <summary>Returns the anonymous (public) dashboard view.</summary>
    [HttpGet]
    public IActionResult AnonymousIndex()
    {
        var loginedAccount = _sessionService.GetAccount();

        if (loginedAccount == null) return View();
        return loginedAccount.Role switch
        {
            Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
            Role.User => RedirectToAction("UserIndex", "DashBoard"),
            _ => View()
        };
    }
    #endregion
}
