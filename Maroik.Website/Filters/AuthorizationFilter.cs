using Maroik.Core.Service.Constants;
using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Attributes;
using Maroik.Website.Constants;
using Maroik.Website.Extensions;
using Maroik.Website.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Maroik.Website.Filters;

/// <summary>
/// Global <b>authorization</b> filter: decides, before model binding and before the action runs,
/// whether the current request may proceed, given the visitor's session role and the seeded
/// navigation menu (a GET must resolve to a menu item for the role; a POST must carry a matching
/// <see cref="RequiredHttpPostAccessAttribute"/> and target a controller present in the role's menu).
/// Denied requests are redirected instead of executing. Being an authorization filter (not an action
/// filter) means a denied request never reaches form / file model binding.
/// <para>
/// It also loads the category / sub-category menu for all three roles (Admin, User, Anonymous) —
/// cached in the distributed cache with a 10-minute sliding expiration — because that data is the
/// input to the decision. The loaded menu is stashed on <c>HttpContext.Items</c> as
/// <see cref="NavigationMenus"/>; <see cref="ViewBagPopulatorFilter"/> publishes it to the ViewBag
/// (there is no controller instance yet at this stage, so it cannot be done here).
/// </para>
/// <para>
/// Ordering: runs at the default filter order (0), i.e. before the global
/// <c>AutoValidateAntiforgeryTokenAttribute</c> (order 1000), so an unauthorized request is rejected
/// before its form body is parsed for an antiforgery token.
/// </para>
/// The controller/action → role map used for POST authorization is derived once from the compiled
/// controllers (see <see cref="_postActionRoles"/>) rather than re-reflected on every request.
/// </summary>
public class AuthorizationFilter(IMenuService menuService, IDistributedCache cache,
    ISessionService sessionService, IAccountService accountService, IAccountPolicyService accountPolicy,
    ILogger<AuthorizationFilter> logger) : IAsyncAuthorizationFilter
{
    // Local aliases of the NavigationCacheKeys entries, one per role x (categories | sub-categories).
    /// <summary>Cache key of the Admin top-level categories.</summary>
    private const string AdminCategoriesCacheKey = NavigationCacheKeys.AdminCategories;
    /// <summary>Cache key of the Admin sub-categories.</summary>
    private const string AdminSubCategoriesCacheKey = NavigationCacheKeys.AdminSubCategories;
    /// <summary>Cache key of the User top-level categories.</summary>
    private const string UserCategoriesCacheKey = NavigationCacheKeys.UserCategories;
    /// <summary>Cache key of the User sub-categories.</summary>
    private const string UserSubCategoriesCacheKey = NavigationCacheKeys.UserSubCategories;
    /// <summary>Cache key of the Anonymous top-level categories.</summary>
    private const string AnonymousCategoriesCacheKey = NavigationCacheKeys.AnonymousCategories;
    /// <summary>Cache key of the Anonymous sub-categories.</summary>
    private const string AnonymousSubCategoriesCacheKey = NavigationCacheKeys.AnonymousSubCategories;

    /// <summary>Every MVC controller type in this assembly, reflected once for <see cref="BuildPostActionRoleMap"/>.</summary>
    private static readonly IReadOnlyList<Type> _cachedControllerTypes =
    [
        .. Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(Controller).IsAssignableFrom(type))
    ];

    /// <summary>
    /// (controller, action) POST endpoint → the set of role strings its
    /// <see cref="RequiredHttpPostAccessAttribute"/> attributes grant. Only actions that also carry
    /// <c>[HttpPost]</c> and <c>[ValidateAntiForgeryToken]</c> are included. The compiled set of
    /// actions and their attributes never changes at runtime, so this is built once instead of
    /// re-scanning every controller's methods and custom attributes on every POST request.
    /// </summary>
    private static readonly FrozenDictionary<(string Controller, string Action), FrozenSet<string>> _postActionRoles =
        BuildPostActionRoleMap();

    /// <summary>The routing name of <see cref="Controllers.AccountController"/>.</summary>
    private const string AccountControllerName = "Account";
    /// <summary>The routing name of <see cref="Controllers.ManagementController"/>.</summary>
    private const string ManagementControllerName = "Management";
    /// <summary>The routing name of <see cref="Controllers.ExceptionController"/>.</summary>
    private const string ExceptionControllerName = "Exception";
    /// <summary>The routing name of <see cref="Controllers.DashboardController"/>.</summary>
    private const string DashboardControllerName = "Dashboard";

    /// <summary>
    /// The <c>AccountController</c> pages a signed-out visitor may open without a navigation-menu match: login, registration,
    /// e-mail confirmation, the consent form and the forgot / reset-password flow.
    /// </summary>
    private static readonly FrozenSet<string> _anonymousAccountPages = new HashSet<string>
    {
        nameof(Controllers.AccountController.Login),
        nameof(Controllers.AccountController.Register),
        nameof(Controllers.AccountController.ConfirmEmail),
        nameof(Controllers.AccountController.ConsentForm),
        nameof(Controllers.AccountController.ForgotPassword),
        nameof(Controllers.AccountController.ResetPassword),
    }.ToFrozenSet();

    /// <summary>
    /// The fixed set of <c>AccountController</c> POST endpoints an unauthenticated visitor may POST
    /// to without a navigation-menu match — login, logout, registration / resend, email
    /// confirmation (link + registration password), and the forgot / reset-password flow. Spelled out explicitly (not reflected off the controller) so
    /// this anonymous surface mirrors the GET allow-list <see cref="_anonymousAccountPages"/>:
    /// adding a new POST action to <c>AccountController</c> does NOT silently widen anonymous access
    /// — it stays denied (redirect to Dashboard) until it is added here on purpose. Every entry must
    /// still carry <c>[HttpPost]</c> + <c>[ValidateAntiForgeryToken]</c>, enforced by
    /// <c>WebsiteArchitectureTests.PostActions_MustCarry_AntiforgeryAndRoleAttributes</c>.
    /// </summary>
    private static readonly FrozenSet<(string Controller, string Action)> _anonymousAccountPostActions =
        new HashSet<(string Controller, string Action)>
        {
            (AccountControllerName, nameof(Controllers.AccountController.Login)),
            (AccountControllerName, nameof(Controllers.AccountController.Logout)),
            (AccountControllerName, nameof(Controllers.AccountController.Register)),
            (AccountControllerName, nameof(Controllers.AccountController.ConfirmEmail)),
            (AccountControllerName, nameof(Controllers.AccountController.ForgotPassword)),
            (AccountControllerName, nameof(Controllers.AccountController.ResetPassword)),
        }.ToFrozenSet();

    /// <summary>The routing name of <paramref name="controllerType"/> (its type name minus the "Controller" suffix).</summary>
    private static string ControllerName(Type controllerType) =>
        controllerType.Name.EndsWith("Controller", StringComparison.Ordinal)
            ? controllerType.Name[..^"Controller".Length]
            : controllerType.Name;

    /// <summary>True when <paramref name="method"/> is a public action carrying both <c>[HttpPost]</c> and <c>[ValidateAntiForgeryToken]</c>.</summary>
    private static bool HasPostGuards(MethodInfo method) =>
        method.IsPublic
        && !method.IsDefined(typeof(NonActionAttribute))
        && method.IsDefined(typeof(HttpPostAttribute))
        && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute));

    /// <summary>Builds <see cref="_postActionRoles"/> by reflecting over every controller's guarded POST actions once.</summary>
    private static FrozenDictionary<(string Controller, string Action), FrozenSet<string>> BuildPostActionRoleMap()
    {
        var map = new Dictionary<(string Controller, string Action), FrozenSet<string>>();
        foreach (Type controller in _cachedControllerTypes)
        {
            foreach (MethodInfo method in controller.GetMethods())
            {
                if (!HasPostGuards(method) || !method.IsDefined(typeof(RequiredHttpPostAccessAttribute)))
                    continue;

                FrozenSet<string> roles = method.GetCustomAttributes<RequiredHttpPostAccessAttribute>()
                    .Select(attr => attr.Role)
                    .Where(role => !string.IsNullOrEmpty(role))
                    .ToFrozenSet(StringComparer.Ordinal);

                if (roles.Count > 0)
                    map[(ControllerName(controller), method.Name)] = roles;
            }
        }
        return map.ToFrozenDictionary();
    }

    /// <summary>What a request gets; <see cref="Decide"/> works it out and <see cref="OnAuthorizationAsync"/> carries it out.</summary>
    private enum AccessDecision
    {
        /// <summary>The request proceeds (no <c>Result</c> set).</summary>
        Allow,

        /// <summary>A signed-out visitor who opened an unlisted page goes to the public dashboard; nothing is logged.</summary>
        RedirectVisitorToPublicDashboard,

        /// <summary>The ordinary forced-change flow (every page leads to the change-password form): not a denial.</summary>
        RedirectToPasswordChange,

        /// <summary>A write attempted while a password change is pending: logged, sent to the public dashboard.</summary>
        DenyWhilePasswordChangeIsPending,

        /// <summary>A refused request: logged, sent to the public dashboard.</summary>
        Deny,
    }

    /// <summary>The endpoint a request resolved to, and its HTTP method.</summary>
    private readonly record struct RequestTarget(string Controller, string Action, string Method)
    {
        /// <summary>Whether this is <paramref name="controller"/> / <paramref name="action"/>.</summary>
        public bool Is(string controller, string action) => Controller == controller && Action == action;
    }

    /// <summary>Loads (or restores from cache) the navigation menu data, then allows or denies the request before model binding.</summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        NavigationMenus menus = await LoadNavigationMenusAsync();

        // Hand the menu to ViewBagPopulatorFilter: an authorization filter runs before the controller
        // instance exists, so there is no ViewBag to write to here.
        context.HttpContext.Items[HttpContextItemKeys.NavigationMenus] = menus;

        var target = new RequestTarget(
            context.ActionDescriptor.RouteValues["controller"] ?? "",
            context.ActionDescriptor.RouteValues["action"] ?? "",
            context.HttpContext.Request.Method);
        var sessionAccount = await sessionService.GetAccountAsync(context.HttpContext.RequestAborted); // Check if account session exists after login

        AccountResponse? loggedInAccount = null;
        if (sessionAccount != null)
        {
            loggedInAccount = await accountService.GetAccountByEmailAsync(sessionAccount.Email ?? "", context.HttpContext.RequestAborted); // Re-check account state from DB so that Deleted/Role/SecurityStamp changes take effect on the next request, not only at login

            if (accountPolicy.GetSessionInvalidationReason(loggedInAccount, sessionAccount) is { } reason)
            {
                logger.LogWarning("Session invalidated for {Email}: {Reason}", sessionAccount.Email, reason);
                sessionService.RemoveAccount(); // Invalidate this session immediately
                context.Result = new RedirectResult("/Account/Login");
                return;
            }

            // The one place the signed-in account is loaded per request: ViewBagPopulatorFilter (runs next
            // in the pipeline) and, through HttpContext.GetLoggedInAccount(), every action read this copy.
            context.HttpContext.Items[HttpContextItemKeys.LoggedInAccount] = loggedInAccount;
        }

        switch (Decide(target, loggedInAccount, menus))
        {
            case AccessDecision.Allow:
                return;
            case AccessDecision.RedirectVisitorToPublicDashboard:
                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                return;
            case AccessDecision.RedirectToPasswordChange:
                context.Result = new RedirectResult("/Management/Profile");
                return;
            case AccessDecision.DenyWhilePasswordChangeIsPending:
                logger.LogWarning("Access denied: {Method} {Controller}/{Action} for {Account} while a password change is pending",
                    target.Method, target.Controller, target.Action, loggedInAccount?.Email);
                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                return;
            case AccessDecision.Deny:
            default:
                // A refused request is a security event: record who asked for what, then send them to the start page.
                logger.LogWarning("Access denied: {Method} {Controller}/{Action} for {Account}",
                    target.Method, target.Controller, target.Action, loggedInAccount?.Email ?? "anonymous");
                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                return;
        }
    }

    /// <summary>The navigation menu of all three roles: from the distributed cache when every entry reads, otherwise rebuilt from the database.</summary>
    private async Task<NavigationMenus> LoadNavigationMenusAsync()
    {
        var results = await Task.WhenAll(
            cache.GetAsync(AdminCategoriesCacheKey),
            cache.GetAsync(AdminSubCategoriesCacheKey),
            cache.GetAsync(UserCategoriesCacheKey),
            cache.GetAsync(UserSubCategoriesCacheKey),
            cache.GetAsync(AnonymousCategoriesCacheKey),
            cache.GetAsync(AnonymousSubCategoriesCacheKey));

        if (results[0] != null && results[1] != null && results[2] != null &&
            results[3] != null && results[4] != null && results[5] != null)
        {
            try
            {
                return new NavigationMenus(
                    JsonSerializer.Deserialize<List<CategoryResponse>>(results[0]!)!,
                    JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[1]!)!,
                    JsonSerializer.Deserialize<List<CategoryResponse>>(results[2]!)!,
                    JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[3]!)!,
                    JsonSerializer.Deserialize<List<CategoryResponse>>(results[4]!)!,
                    JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[5]!)!);
            }
            catch (Exception e)
            {
                // A poisoned or shape-changed cache entry must not turn into a 500 on every request
                // for the whole 10-minute TTL — fall through so the rebuild below reads the database,
                // which also overwrites the bad entry. (SessionService.GetAccountAsync
                // guards its own deserialization the same way.)
                logger.LogWarning(e, "Navigation menu cache entry could not be read; rebuilding it from the database");
            }
        }

        var categories = (await menuService.GetAllCategoriesAsync()).ToList();
        var subCategories = (await menuService.GetAllSubCategoriesAsync()).ToList();

        var entryOptions = new DistributedCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));

        var menus = new NavigationMenus(
            [.. categories.Where(c => c.Role == AccountRoles.Admin)],
            [.. subCategories.Where(s => s.Role == AccountRoles.Admin)],
            [.. categories.Where(c => c.Role == AccountRoles.User)],
            [.. subCategories.Where(s => s.Role == AccountRoles.User)],
            [.. categories.Where(c => c.Role == AccountRoles.Anonymous)],
            [.. subCategories.Where(s => s.Role == AccountRoles.Anonymous)]);

        await Task.WhenAll(
            cache.SetAsync(AdminCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.AdminCategories), entryOptions),
            cache.SetAsync(AdminSubCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.AdminSubCategories), entryOptions),
            cache.SetAsync(UserCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.UserCategories), entryOptions),
            cache.SetAsync(UserSubCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.UserSubCategories), entryOptions),
            cache.SetAsync(AnonymousCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.AnonymousCategories), entryOptions),
            cache.SetAsync(AnonymousSubCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(menus.AnonymousSubCategories), entryOptions));

        return menus;
    }

    /// <summary>The allow / deny decision for <paramref name="target"/>: no I/O, only the request, the account and the menu.</summary>
    /// <param name="target">The endpoint and method of the request.</param>
    /// <param name="account">The validated signed-in account, or <see langword="null"/> for a signed-out visitor.</param>
    /// <param name="menus">The navigation menu of all three roles.</param>
    private static AccessDecision Decide(RequestTarget target, AccountResponse? account, NavigationMenus menus)
    {
        if (account is { MustChangePassword: true })
            return DecideWhilePasswordChangeIsPending(target);

        // UseExceptionHandler re-executes /Exception/Error with the ORIGINAL request's method, so a
        // failed POST/PUT/DELETE arrives here as that method. The error page is inert (it changes no
        // state and shows only a request id), so it is reachable for every method and every role;
        // without this, the POST branches below would redirect the user away from the error page.
        if (target.Is(ExceptionControllerName, nameof(Controllers.ExceptionController.Error)))
            return AccessDecision.Allow;

        return target.Method.ToUpperInvariant() switch
        {
            // GET method (block page access only!!)
            "GET" => account == null ? DecideVisitorGet(target, menus) : DecideSignedInGet(target, account, menus),
            "POST" => account == null ? DecideVisitorPost(target, menus) : DecideSignedInPost(target, account, menus),
            // (Reject all methods like PATCH / DELETE etc.)
            _ => AccessDecision.Deny,
        };
    }

    /// <summary>
    /// An admin-forced password change locks the account to the profile page (where the change-password form lives) and
    /// logout, until UpdateProfilePassword clears the flag.
    /// </summary>
    private static AccessDecision DecideWhilePasswordChangeIsPending(RequestTarget target)
    {
        bool isAllowed =
            target.Is(ManagementControllerName, nameof(Controllers.ManagementController.Profile)) ||
            target.Is(ManagementControllerName, nameof(Controllers.ManagementController.UpdateProfilePassword)) ||
            target.Is(AccountControllerName, nameof(Controllers.AccountController.Logout)) ||
            target.Is(ExceptionControllerName, nameof(Controllers.ExceptionController.Error));

        if (isAllowed)
            return AccessDecision.Allow;

        return string.Equals(target.Method, "GET", StringComparison.OrdinalIgnoreCase)
            ? AccessDecision.RedirectToPasswordChange
            : AccessDecision.DenyWhilePasswordChangeIsPending;
    }

    /// <summary>A signed-out GET: the fixed account pages, or a page of the anonymous menu.</summary>
    private static AccessDecision DecideVisitorGet(RequestTarget target, NavigationMenus menus)
    {
        if (target.Controller == AccountControllerName && _anonymousAccountPages.Contains(target.Action))
            return AccessDecision.Allow;

        return menus.AnonymousCategories.ResolveActiveMenuItem(menus.AnonymousSubCategories, target.Controller, target.Action).IsMatch
            ? AccessDecision.Allow
            : AccessDecision.RedirectVisitorToPublicDashboard;
    }

    /// <summary>A signed-in GET: the initial screen, or a page of the role's menu.</summary>
    private static AccessDecision DecideSignedInGet(RequestTarget target, AccountResponse account, NavigationMenus menus)
    {
        // Initial screen
        if (target.Is(DashboardControllerName, nameof(Controllers.DashboardController.AnonymousIndex)))
            return AccessDecision.Allow;

        // Admin and User share identical resolution logic — only which category/sub-category list is searched differs by
        // role. Role is a plain string, not an enum — an account somehow persisted with neither Admin nor User must still
        // get an explicit denial.
        var match = account.Role switch
        {
            AccountRoles.Admin => menus.AdminCategories.ResolveActiveMenuItem(menus.AdminSubCategories, target.Controller, target.Action),
            AccountRoles.User => menus.UserCategories.ResolveActiveMenuItem(menus.UserSubCategories, target.Controller, target.Action),
            _ => null,
        };

        return match is { IsMatch: true } ? AccessDecision.Allow : AccessDecision.Deny;
    }

    /// <summary>A signed-out POST: the fixed account endpoints, or an action marked for the Anonymous role on a controller of the anonymous menu.</summary>
    private static AccessDecision DecideVisitorPost(RequestTarget target, NavigationMenus menus)
    {
        // AccountController's own POST actions (login, register, forgot/reset password …)
        // are always reachable to an anonymous visitor.
        if (_anonymousAccountPostActions.Contains((target.Controller, target.Action)))
            return AccessDecision.Allow;

        return IsPostAllowedFor(AccountRoles.Anonymous, menus.AnonymousCategories, target) ? AccessDecision.Allow : AccessDecision.Deny;
    }

    /// <summary>A signed-in POST: logout, or an action marked for the account's role on a controller of that role's menu.</summary>
    private static AccessDecision DecideSignedInPost(RequestTarget target, AccountResponse account, NavigationMenus menus)
    {
        // Logout is a POST (antiforgery-protected) endpoint on AccountController, which is
        // not part of any role's navigation menu — allow it explicitly for any signed-in
        // account regardless of role.
        if (target.Is(AccountControllerName, nameof(Controllers.AccountController.Logout)))
            return AccessDecision.Allow;

        // Role is a plain string, not an enum — an account somehow persisted with neither Admin nor User must still get an
        // explicit denial.
        return account.Role switch
        {
            AccountRoles.Admin when IsPostAllowedFor(AccountRoles.Admin, menus.AdminCategories, target) => AccessDecision.Allow,
            AccountRoles.User when IsPostAllowedFor(AccountRoles.User, menus.UserCategories, target) => AccessDecision.Allow,
            _ => AccessDecision.Deny,
        };
    }

    /// <summary>
    /// Whether the action carries <c>[RequiredHttpPostAccess(Role = <paramref name="role"/>)]</c> AND its controller appears in
    /// <paramref name="categories"/>, the navigation menu of that role.
    /// </summary>
    private static bool IsPostAllowedFor(string role, List<CategoryResponse> categories, RequestTarget target) =>
        _postActionRoles.TryGetValue((target.Controller, target.Action), out FrozenSet<string>? allowedRoles)
        && allowedRoles.Contains(role)
        && categories.Any(category => category.Controller == target.Controller);
}
