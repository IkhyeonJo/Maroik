using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Controllers;
using Maroik.Website.Models.ViewModels.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// The culture cookie follows the hosting profile like every other cookie: the action sets no Secure flag of its own, and the cookie
/// policy middleware adds it where the site is reached over HTTPS (Tunnel, PortForwarding). A Secure flag set here was kept over
/// plain HTTP in NoDomain mode, where a browser drops a Secure cookie, so changing the language did nothing there.
/// </summary>
public class DashboardControllerCultureCookieTests
{
    /// <summary>Over plain HTTP (NoDomain) the culture cookie is written without the Secure flag, so a browser keeps it.</summary>
    [Fact]
    public void CultureManagement_SetsNoSecureFlagOfItsOwn()
    {
        using var controller = new DashboardController(Mock.Of<IDashboardService>(), Mock.Of<IProfileService>(),
            new FakeLogger<DashboardController>(), new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { Request = { Scheme = "http" } } }
        };

        controller.CultureManagement(new LoginInputViewModel { Culture = "ko-KR" });

        string cookie = Assert.Single(controller.Response.Headers.SetCookie)!;
        Assert.StartsWith(".AspNetCore.Culture=", cookie, StringComparison.Ordinal);
        Assert.DoesNotContain("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }
}
