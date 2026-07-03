using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.Website.Contracts;
using Maroik.Website.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Website.Tests.Filters;

/// <summary>
/// Unit tests for <see cref="ViewBagPopulatorFilter"/>.
/// All service dependencies are replaced with Moq mocks.
/// </summary>
public class ViewBagPopulatorFilterTests
{
    private readonly Mock<IAccountService> _accountService = new();
    private readonly Mock<IDashboardService> _dashboardService = new();
    private readonly Mock<ISessionService> _sessionService = new();
    private readonly IOptions<ServerSetting> _serverSettings = Options.Create(new ServerSetting
    {
        DomainName = "https://test.maroik.com",
        MaxAttachedFileSizeBytes = 10_485_760,
        NoticeMaturityDateDay = 7
    });

    private ViewBagPopulatorFilter CreateSut() =>
        new(_accountService.Object, _dashboardService.Object, _serverSettings, _sessionService.Object);

    private static (ActionExecutingContext context, Controller controller) BuildContext(
        string path = "/Test/Index",
        string? cultureName = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.Path = path;

        if (cultureName != null)
        {
            var cultureFeature = new Mock<IRequestCultureFeature>();
            cultureFeature.Setup(f => f.RequestCulture)
                .Returns(new RequestCulture(cultureName));
            httpContext.Features.Set(cultureFeature.Object);
        }

        var controller = new FakeController();
        var actionDescriptor = new ControllerActionDescriptor();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = actionDescriptor
        };

        var actionContext = new ActionContext(httpContext, new RouteData(), actionDescriptor);
        var executingContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller);

        return (executingContext, controller);
    }

    private static ActionExecutionDelegate EmptyNext() =>
        () => Task.FromResult(new ActionExecutedContext(
            new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()),
            new List<IFilterMetadata>(),
            null!));

    // -- ServerSettings -------------------------------------------------------

    /// <summary>Verifies that <c>ViewBag.DomainName</c> is populated from server settings.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsDomainName_FromServerSettings()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal("https://test.maroik.com", controller.ViewBag.DomainName);
    }

    /// <summary>Verifies that <c>ViewBag.MaxAttachedFileSizeBytes</c> is populated from server settings.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsMaxAttachedFileSizeBytes_FromServerSettings()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal(10_485_760L, controller.ViewBag.MaxAttachedFileSizeBytes);
    }

    // -- Anonymous session (no session account) --------------------------------

    /// <summary>Verifies that <c>ViewBag.LoggedInAccount</c> defaults to anonymous when no session exists.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsAnonymousAccount_WhenNoSession()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        AccountResponse account = controller.ViewBag.LoggedInAccount;
        Assert.Equal(Role.Anonymous, account.Role);
    }

    /// <summary>Verifies that notice counts are zero when no session exists.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsZeroNoticeCounts_WhenNoSession()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal(0, controller.ViewBag.FixedIncomesNoticedCount);
        Assert.Equal(0, controller.ViewBag.FixedExpenditureNoticedCount);
        Assert.Equal(0, controller.ViewBag.FixedIncomesExpiredCount);
        Assert.Equal(0, controller.ViewBag.FixedExpenditureExpiredCount);
    }

    // -- Logged-in User session ------------------------------------------------

    /// <summary>Verifies that <c>ViewBag.LoggedInAccount</c> is populated from the account service when session exists.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsLoggedInAccount_WhenSessionExists()
    {
        var sessionAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };
        var dbAccount = new AccountResponse { Email = "user@test.com", Nickname = "TestUser", Role = Role.User };

        _sessionService.Setup(s => s.GetAccount()).Returns(sessionAccount);
        _accountService
            .Setup(a => a.GetAccountByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbAccount);
        _dashboardService
            .Setup(d => d.GetNoticeCountsAsync("user@test.com", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationCounts { FixedIncomesNoticed = 2, FixedExpendituresNoticed = 3, FixedIncomesExpired = 1, FixedExpendituresExpired = 4 });

        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        AccountResponse account = controller.ViewBag.LoggedInAccount;
        Assert.Equal("TestUser", account.Nickname);
        Assert.Equal(Role.User, account.Role);
    }

    /// <summary>Verifies that notice counts are populated from the dashboard service for a logged-in user.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsNoticeCounts_WhenUserIsLoggedIn()
    {
        var sessionAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };
        var dbAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };

        _sessionService.Setup(s => s.GetAccount()).Returns(sessionAccount);
        _accountService
            .Setup(a => a.GetAccountByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbAccount);
        _dashboardService
            .Setup(d => d.GetNoticeCountsAsync("user@test.com", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationCounts { FixedIncomesNoticed = 2, FixedExpendituresNoticed = 3, FixedIncomesExpired = 1, FixedExpendituresExpired = 4 });

        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal(2, controller.ViewBag.FixedIncomesNoticedCount);
        Assert.Equal(3, controller.ViewBag.FixedExpenditureNoticedCount);
        Assert.Equal(1, controller.ViewBag.FixedIncomesExpiredCount);
        Assert.Equal(4, controller.ViewBag.FixedExpenditureExpiredCount);
    }

    // -- Culture & ReturnUri --------------------------------------------------

    /// <summary>Verifies that <c>ViewBag.CurrentCulture</c> falls back to "en-US" when no culture feature is set.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_DefaultsCultureToEnUs_WhenNoCultureFeature()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext();

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal("en-US", controller.ViewBag.CurrentCulture);
    }

    /// <summary>Verifies that <c>ViewBag.CurrentCulture</c> reflects the request culture feature.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsCulture_FromRequestCultureFeature()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext(cultureName: "ko-KR");

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal("ko-KR", controller.ViewBag.CurrentCulture);
    }

    /// <summary>Verifies that <c>ViewBag.ReturnUri</c> is populated from the request path.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_SetsReturnUri_FromRequestPath()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var (context, controller) = BuildContext(path: "/Forum/FreeForum");

        await CreateSut().OnActionExecutionAsync(context, EmptyNext());

        Assert.Equal("/Forum/FreeForum", controller.ViewBag.ReturnUri);
    }

    // -- Resilience -----------------------------------------------------------

    /// <summary>Verifies that a faulting account service does not throw — falls back to anonymous account.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_FallsBackToAnonymous_WhenAccountServiceThrows()
    {
        var sessionAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };
        _sessionService.Setup(s => s.GetAccount()).Returns(sessionAccount);
        _accountService
            .Setup(a => a.GetAccountByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var (context, controller) = BuildContext();

        var ex = await Record.ExceptionAsync(() => CreateSut().OnActionExecutionAsync(context, EmptyNext()));

        Assert.Null(ex);
        AccountResponse account = controller.ViewBag.LoggedInAccount;
        Assert.Equal(Role.Anonymous, account.Role);
    }

    /// <summary>Verifies that a faulting dashboard service does not throw — notice counts stay zero.</summary>
    [Fact]
    public async Task OnActionExecutionAsync_KeepsZeroNoticeCounts_WhenDashboardServiceThrows()
    {
        var sessionAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };
        var dbAccount = new AccountResponse { Email = "user@test.com", Role = Role.User };

        _sessionService.Setup(s => s.GetAccount()).Returns(sessionAccount);
        _accountService
            .Setup(a => a.GetAccountByEmailAsync("user@test.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbAccount);
        _dashboardService
            .Setup(d => d.GetNoticeCountsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var (context, controller) = BuildContext();

        var ex = await Record.ExceptionAsync(() => CreateSut().OnActionExecutionAsync(context, EmptyNext()));

        Assert.Null(ex);
        Assert.Equal(0, controller.ViewBag.FixedIncomesNoticedCount);
    }
}

/// <summary>Minimal <see cref="Controller"/> subclass used as the action target in filter tests.</summary>
internal class FakeController : Controller
{
    public IActionResult Index() => Ok();
}
