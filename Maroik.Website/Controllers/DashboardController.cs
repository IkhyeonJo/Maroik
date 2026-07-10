using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Attributes;
using Maroik.Website.Contracts;
using Maroik.Website.Mappings;
using Maroik.Website.Models.ViewModels.Account;
using Maroik.Website.Models.ViewModels.Dashboard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.Website.Controllers;

/// <summary>
/// Serves the dashboard index views (Admin / User / Anonymous) and handles
/// language-culture changes and the user's default monetary-unit preference.
/// </summary>
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IProfileService _profileService;
    private readonly ISessionService _sessionService;

    private const string HostResourceFilePath = "/app/Maroik.Log/HostResourceInfo.txt";
    private const string DockerResourceFilePath = "/app/Maroik.Log/DockerResourceInfo.txt";

    /// <summary>Initializes a new instance of <see cref="DashboardController"/> with the required dependencies.</summary>
    public DashboardController(
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
        var loggedInAccount = _sessionService.GetAccount();
        string? email = loggedInAccount?.Email;
        string? timeZoneId = loggedInAccount?.TimeZoneIanaId;

        // The public demo account always shows a fixed, pre-seeded period so visitors see
        // consistent sample data regardless of when they browse the demo.
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
        var loggedInAccount = _sessionService.GetAccount();

        if (loggedInAccount == null) return View();
        return loggedInAccount.Role switch
        {
            Role.Admin => RedirectToAction("AdminIndex", "Dashboard"),
            Role.User => RedirectToAction("UserIndex", "Dashboard"),
            _ => View()
        };
    }
    #endregion
}
