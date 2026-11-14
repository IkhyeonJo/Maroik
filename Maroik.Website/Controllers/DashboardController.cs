using Maroik.Core.Service.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Attributes;
using Maroik.Website.Constants;
using Maroik.Website.Extensions;
using Maroik.Website.Mappers;
using Maroik.Website.Models.ViewModels.Accounts;
using Maroik.Website.Models.ViewModels.Dashboards;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.Website.Controllers;

/// <summary>
/// Serves the dashboard index views (Admin / User / Anonymous) and handles
/// language-culture changes and the user's default monetary-unit preference.
/// </summary>
public class DashboardController(
    IDashboardService dashboardService,
    IProfileService profileService,
    ILogger<DashboardController> logger,
    TimeProvider timeProvider) : Controller
{
    #region Language change
    /// <summary>Sets the UI culture cookie to the requested locale.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = AccountRoles.Admin)]
    [RequiredHttpPostAccess(Role = AccountRoles.User)]
    [RequiredHttpPostAccess(Role = AccountRoles.Anonymous)]
    public IActionResult CultureManagement([FromBody] LoginInputViewModel loginInputViewModel)
    {
        // Only the cultures the app actually supports (CulturePolicy.SupportedCultures — the same
        // list Program.cs configures RequestLocalizationOptions with). Anything else (a bogus value
        // would make new RequestCulture(...) / new CultureInfo(...) throw an unhandled
        // CultureNotFoundException) is rejected here rather than written to the cookie.
        string? culture = loginInputViewModel.Culture;
        if (string.IsNullOrEmpty(culture) || !CulturePolicy.SupportedCultures.Contains(culture, StringComparer.OrdinalIgnoreCase))
            return Json(new { result = false });

        Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            // No Secure flag of its own: the cookie policy middleware applies the hosting profile's (Secure behind HTTPS, none in
            // NoDomain, where a browser would drop a Secure cookie set over plain HTTP).
            new CookieOptions { Expires = timeProvider.GetUtcNow().AddDays(30), HttpOnly = true, SameSite = SameSiteMode.Strict });

        return Json(new { result = true });
    }
    #endregion

    #region Admin
    /// <summary>Returns the admin dashboard view.</summary>
    [HttpGet]
    public IActionResult AdminIndex()
    {
        var resource = dashboardService.GetServerResourceSummary();

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
        AccountResponse loggedInAccount = HttpContext.GetLoggedInAccount();
        string? email = loggedInAccount.Email;
        string? timeZoneId = loggedInAccount.TimeZoneIanaId;

        var summary = await dashboardService.GetSummaryAsync(email!, year, month, timeZoneId!, ct);
        return View(summary.ToViewModel());
    }
    #endregion

    #region Update
    /// <summary>Updates the logged-in user's default monetary unit preference.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = AccountRoles.User)]
    public async Task<IActionResult> UserUpdateDefaultMonetary([FromBody] UserIndexInputViewModel userIndexInputViewModel, CancellationToken ct)
    {
        string email = HttpContext.GetLoggedInAccount().Email ?? "";
        try
        {
            await profileService.UpdateDefaultMonetaryUnitAsync(email, userIndexInputViewModel.DefaultMonetaryUnit, ct);
            return Json(new { result = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update default monetary unit for account {AccountEmail}", email);
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
        AccountResponse loggedInAccount = HttpContext.GetLoggedInAccount();

        return loggedInAccount.Role switch
        {
            AccountRoles.Admin => RedirectToAction("AdminIndex", "Dashboard"),
            AccountRoles.User => RedirectToAction("UserIndex", "Dashboard"),
            _ => View()
        };
    }
    #endregion
}
