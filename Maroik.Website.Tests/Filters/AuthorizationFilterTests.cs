using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Contracts;
using Maroik.Website.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Maroik.Website.Tests.Filters;

/// <summary>
/// Unit tests for <see cref="AuthorizationFilter"/>, focused purely on the GET/POST allow/deny
/// gating driven by <c>MenuNavigationMapper.ResolveActiveMenuItem</c>. This filter no longer sets
/// any view-rendering ViewBag values itself (that responsibility moved to
/// <c>ViewBagPopulatorFilter</c>, which runs after it) — it only decides whether the request is
/// allowed to proceed. All service dependencies are replaced with Moq mocks; the distributed
/// cache always misses so every test exercises the <see cref="IMenuService"/> fallback path.
/// </summary>
public class AuthorizationFilterTests
{
    private readonly Mock<IMenuService> _menuService = new();
    private readonly Mock<IDistributedCache> _cache = new();
    private readonly Mock<ISessionService> _sessionService = new();
    private readonly Mock<IAccountService> _accountService = new();

    public AuthorizationFilterTests()
    {
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
    }

    private AuthorizationFilter CreateSut() =>
        new(_menuService.Object, _cache.Object, _sessionService.Object, _accountService.Object);

    private static CategoryResponse MakeCategory(long id, string role, string controller, string? action, long order = 0) => new()
    {
        Id = id,
        DisplayName = $"Category{id}",
        Role = role,
        Controller = controller,
        Action = action,
        Order = order
    };

    private static SubCategoryResponse MakeSubCategory(long id, long categoryId, string role, string action, long order = 0) => new()
    {
        Id = id,
        CategoryId = categoryId,
        DisplayName = $"SubCategory{id}",
        Role = role,
        Action = action,
        Order = order
    };

    private void SetupMenu(IEnumerable<CategoryResponse> categories, IEnumerable<SubCategoryResponse> subCategories)
    {
        _menuService.Setup(m => m.GetAllCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(categories);
        _menuService.Setup(m => m.GetAllSubCategoriesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(subCategories);
    }

    private static (ActionExecutingContext context, Controller controller) BuildContext(
        string controllerName, string actionName, string httpMethod = "GET")
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = sp };
        httpContext.Request.Method = httpMethod;

        var controller = new FakeController();
        var actionDescriptor = new ControllerActionDescriptor
        {
            RouteValues = new Dictionary<string, string?>
            {
                ["controller"] = controllerName,
                ["action"] = actionName
            }
        };
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

    private static ActionExecutionDelegate NextCalled(Action onNext) =>
        () =>
        {
            onNext();
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()),
                new List<IFilterMetadata>(),
                null!));
        };

    // -- GET, not logged in -------------------------------------------------------

    [Fact]
    public async Task OnActionExecutionAsync_Get_NotLoggedIn_BypassRoute_AlwaysAllowed()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        SetupMenu([], []);
        var (context, _) = BuildContext("Account", "Login");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_NotLoggedIn_SingleCategoryMatch_Allows()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var category = MakeCategory(1, Role.Anonymous, "Dashboard", "AnonymousIndex");
        SetupMenu([category], []);
        var (context, _) = BuildContext("Dashboard", "AnonymousIndex");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_NotLoggedIn_SubCategoryMatch_Allows()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var category = MakeCategory(1, Role.Anonymous, "Notice", null); // parent category (no direct Action = has children)
        var subCategory = MakeSubCategory(10, categoryId: 1, role: Role.Anonymous, action: "FixedIncome");
        SetupMenu([category], [subCategory]);
        var (context, _) = BuildContext("Notice", "FixedIncome");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_NotLoggedIn_NoMatch_Redirects()
    {
        _sessionService.Setup(s => s.GetAccount()).Returns((AccountResponse?)null);
        var category = MakeCategory(1, Role.Anonymous, "Dashboard", "AnonymousIndex");
        SetupMenu([category], []);
        var (context, _) = BuildContext("Forum", "FreeForum");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectResult>(context.Result);
        Assert.Equal("/Dashboard/AnonymousIndex", redirect.Url);
    }

    // -- GET, Admin logged in ------------------------------------------------------

    [Fact]
    public async Task OnActionExecutionAsync_Get_AdminLoggedIn_SingleCategoryMatch_Allows()
    {
        var admin = new AccountResponse { Email = "admin@test.com", Nickname = "Admin", Role = Role.Admin };
        _sessionService.Setup(s => s.GetAccount()).Returns(admin);
        _accountService.Setup(a => a.GetAccountByEmailAsync("admin@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        var category = MakeCategory(1, Role.Admin, "Management", "Account");
        SetupMenu([category], []);
        var (context, _) = BuildContext("Management", "Account");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_AdminLoggedIn_SubCategoryMatch_Allows()
    {
        var admin = new AccountResponse { Email = "admin@test.com", Nickname = "Admin", Role = Role.Admin };
        _sessionService.Setup(s => s.GetAccount()).Returns(admin);
        _accountService.Setup(a => a.GetAccountByEmailAsync("admin@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        var category = MakeCategory(1, Role.Admin, "Notice", null); // parent category (no direct Action = has children)
        var subCategory = MakeSubCategory(10, categoryId: 1, role: Role.Admin, action: "FixedIncome");
        SetupMenu([category], [subCategory]);
        var (context, _) = BuildContext("Notice", "FixedIncome");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_AdminLoggedIn_NoMatch_Redirects()
    {
        var admin = new AccountResponse { Email = "admin@test.com", Nickname = "Admin", Role = Role.Admin };
        _sessionService.Setup(s => s.GetAccount()).Returns(admin);
        _accountService.Setup(a => a.GetAccountByEmailAsync("admin@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(admin);
        var category = MakeCategory(1, Role.Admin, "Management", "Account");
        SetupMenu([category], []);
        var (context, _) = BuildContext("Unregistered", "Action");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectResult>(context.Result);
        Assert.Equal("/Dashboard/AnonymousIndex", redirect.Url);
    }

    // -- GET, User logged in --------------------------------------------------------

    [Fact]
    public async Task OnActionExecutionAsync_Get_UserLoggedIn_SingleCategoryMatch_Allows()
    {
        var user = new AccountResponse { Email = "user@test.com", Nickname = "User", Role = Role.User };
        _sessionService.Setup(s => s.GetAccount()).Returns(user);
        _accountService.Setup(a => a.GetAccountByEmailAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var category = MakeCategory(1, Role.User, "AccountBook", "Income");
        SetupMenu([category], []);
        var (context, _) = BuildContext("AccountBook", "Income");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnActionExecutionAsync_Get_UserLoggedIn_NoMatch_Redirects()
    {
        var user = new AccountResponse { Email = "user@test.com", Nickname = "User", Role = Role.User };
        _sessionService.Setup(s => s.GetAccount()).Returns(user);
        _accountService.Setup(a => a.GetAccountByEmailAsync("user@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var category = MakeCategory(1, Role.User, "AccountBook", "Income");
        SetupMenu([category], []);
        var (context, _) = BuildContext("Management", "Account"); // Admin-only route

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => { }));

        var redirect = Assert.IsType<RedirectResult>(context.Result);
        Assert.Equal("/Dashboard/AnonymousIndex", redirect.Url);
    }

    // -- Session validity (unrelated to menu resolution, still gated by this filter) --

    [Fact]
    public async Task OnActionExecutionAsync_LoggedInAccountDeletedInDb_RedirectsToLoginAndClearsSession()
    {
        var sessionAccount = new AccountResponse { Email = "gone@test.com", Nickname = "Gone", Role = Role.User };
        var dbAccount = new AccountResponse { Email = "gone@test.com", Nickname = "Gone", Role = Role.User, Deleted = true };
        _sessionService.Setup(s => s.GetAccount()).Returns(sessionAccount);
        _accountService.Setup(a => a.GetAccountByEmailAsync("gone@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(dbAccount);
        SetupMenu([], []);
        var (context, _) = BuildContext("Dashboard", "UserIndex");
        bool nextCalled = false;

        await CreateSut().OnActionExecutionAsync(context, NextCalled(() => nextCalled = true));

        Assert.False(nextCalled);
        var redirect = Assert.IsType<RedirectResult>(context.Result);
        Assert.Equal("/Account/Login", redirect.Url);
        _sessionService.Verify(s => s.RemoveAccount(), Times.Once);
    }
}
