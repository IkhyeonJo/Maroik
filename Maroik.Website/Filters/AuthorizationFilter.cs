using System.Reflection;
using System.Text.Json;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Attributes;
using Maroik.Website.Constants;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Maroik.Website.Filters;

/// <summary>
/// Global action filter with two jobs, run before every action:
/// <list type="bullet">
///   <item><b>Authorization.</b> Decides whether the current request may run, given the visitor's
///   session role and the seeded navigation menu (a GET must resolve to a menu item for the role;
///   a POST must carry a matching <see cref="RequiredHttpPostAccessAttribute"/> and target a
///   controller present in the role's menu). Denied requests are redirected instead of executing.</item>
///   <item><b>Navigation.</b> Loads the category / sub-category menu for all three roles (Admin,
///   User, Anonymous) — cached in the distributed cache with a 10-minute sliding expiration — and
///   puts it on <c>ViewBag</c>. This data is also the input to the authorization decision above,
///   which is why the two concerns share one filter.</item>
/// </list>
/// The controller/action → role map used for POST authorization is derived once from the compiled
/// controllers (see <see cref="PostActionRoles"/>) rather than re-reflected on every request.
/// </summary>
public class AuthorizationFilter : ActionFilterAttribute
{
    private readonly IMenuService _menuService;
    private readonly IDistributedCache _cache;
    private readonly ISessionService _sessionService;
    private readonly IAccountService _accountService;

    private const string AdminCategoriesCacheKey = NavigationCacheKeys.AdminCategories;
    private const string AdminSubCategoriesCacheKey = NavigationCacheKeys.AdminSubCategories;
    private const string UserCategoriesCacheKey = NavigationCacheKeys.UserCategories;
    private const string UserSubCategoriesCacheKey = NavigationCacheKeys.UserSubCategories;
    private const string AnonymousCategoriesCacheKey = NavigationCacheKeys.AnonymousCategories;
    private const string AnonymousSubCategoriesCacheKey = NavigationCacheKeys.AnonymousSubCategories;

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
    private static readonly IReadOnlyDictionary<(string Controller, string Action), IReadOnlySet<string>> PostActionRoles =
        BuildPostActionRoleMap();

    /// <summary>
    /// (controller, action) pairs for every <c>AccountController</c> POST action carrying
    /// <c>[HttpPost]</c> and <c>[ValidateAntiForgeryToken]</c> — the endpoints an unauthenticated
    /// visitor may POST to (login, register, forgot / reset password, …) without a menu match.
    /// </summary>
    private static readonly IReadOnlySet<(string Controller, string Action)> AnonymousAccountPostActions =
        BuildAnonymousAccountPostActions();

    private static string ControllerName(Type controllerType) =>
        controllerType.Name.EndsWith("Controller", StringComparison.Ordinal)
            ? controllerType.Name[..^"Controller".Length]
            : controllerType.Name;

    private static bool HasPostGuards(MethodInfo method) =>
        method.IsPublic
        && !method.IsDefined(typeof(NonActionAttribute))
        && method.IsDefined(typeof(HttpPostAttribute))
        && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute));

    private static IReadOnlyDictionary<(string, string), IReadOnlySet<string>> BuildPostActionRoleMap()
    {
        var map = new Dictionary<(string, string), IReadOnlySet<string>>();
        foreach (Type controller in _cachedControllerTypes)
        {
            foreach (MethodInfo method in controller.GetMethods())
            {
                if (!HasPostGuards(method) || !method.IsDefined(typeof(RequiredHttpPostAccessAttribute)))
                    continue;

                IReadOnlySet<string> roles = method.GetCustomAttributes<RequiredHttpPostAccessAttribute>()
                    .Select(attr => attr.Role)
                    .Where(role => !string.IsNullOrEmpty(role))
                    .ToHashSet(StringComparer.Ordinal);

                if (roles.Count > 0)
                    map[(ControllerName(controller), method.Name)] = roles;
            }
        }
        return map;
    }

    private static IReadOnlySet<(string, string)> BuildAnonymousAccountPostActions()
    {
        var set = new HashSet<(string, string)>();
        foreach (Type controller in _cachedControllerTypes.Where(type => type.Name == "AccountController"))
            foreach (MethodInfo method in controller.GetMethods())
                if (HasPostGuards(method))
                    set.Add((ControllerName(controller), method.Name));
        return set;
    }

    /// <summary>Initializes a new instance of <see cref="AuthorizationFilter"/> with the required dependencies.</summary>
    /// <param name="menuService">Service used to load category and sub-category navigation data.</param>
    /// <param name="cache">Distributed cache (Valkey) used to store the navigation menu for 10 minutes.</param>
    /// <param name="sessionService">Service used to retrieve the currently logged-in account from the session.</param>
    /// <param name="accountService">Service used to re-verify the logged-in account's current state from the database.</param>
    public AuthorizationFilter(IMenuService menuService, IDistributedCache cache, ISessionService sessionService, IAccountService accountService)
    {
        _menuService = menuService;
        _cache = cache;
        _sessionService = sessionService;
        _accountService = accountService;
    }

    /// <summary>Loads (or restores from cache) the navigation menu data and populates ViewBag before the action executes.</summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var results = await Task.WhenAll(
            _cache.GetAsync(AdminCategoriesCacheKey),
            _cache.GetAsync(AdminSubCategoriesCacheKey),
            _cache.GetAsync(UserCategoriesCacheKey),
            _cache.GetAsync(UserSubCategoriesCacheKey),
            _cache.GetAsync(AnonymousCategoriesCacheKey),
            _cache.GetAsync(AnonymousSubCategoriesCacheKey));

        List<CategoryResponse> adminCategories = [];
        List<SubCategoryResponse> adminSubCategories = [];
        List<CategoryResponse> userCategories = [];
        List<SubCategoryResponse> userSubCategories = [];
        List<CategoryResponse> anonymousCategories = [];
        List<SubCategoryResponse> anonymousSubCategories = [];

        bool menusLoaded = false;
        if (results[0] != null && results[1] != null && results[2] != null &&
            results[3] != null && results[4] != null && results[5] != null)
        {
            try
            {
                adminCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[0]!)!;
                adminSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[1]!)!;
                userCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[2]!)!;
                userSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[3]!)!;
                anonymousCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[4]!)!;
                anonymousSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[5]!)!;
                menusLoaded = true;
            }
            catch (Exception)
            {
                // A poisoned or shape-changed cache entry must not turn into a 500 on every request
                // for the whole 10-minute TTL — leave menusLoaded false so the block below rebuilds
                // from the database, which also overwrites the bad entry. (SessionService.GetAccount
                // guards its own deserialization the same way.)
            }
        }

        if (!menusLoaded)
        {
            var categories = (await _menuService.GetAllCategoriesAsync()).ToList();
            var subCategories = (await _menuService.GetAllSubCategoriesAsync()).ToList();

            var entryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            adminCategories = [.. categories.Where(c => c.Role == Role.Admin)];
            adminSubCategories = [.. subCategories.Where(s => s.Role == Role.Admin)];
            userCategories = [.. categories.Where(c => c.Role == Role.User)];
            userSubCategories = [.. subCategories.Where(s => s.Role == Role.User)];
            anonymousCategories = [.. categories.Where(c => c.Role == Role.Anonymous)];
            anonymousSubCategories = [.. subCategories.Where(s => s.Role == Role.Anonymous)];

            await Task.WhenAll(
                _cache.SetAsync(AdminCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(adminCategories),
                    entryOptions),
                _cache.SetAsync(AdminSubCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(adminSubCategories),
                    entryOptions),
                _cache.SetAsync(UserCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(userCategories),
                    entryOptions),
                _cache.SetAsync(UserSubCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(userSubCategories),
                    entryOptions),
                _cache.SetAsync(AnonymousCategoriesCacheKey, JsonSerializer.SerializeToUtf8Bytes(anonymousCategories),
                    entryOptions),
                _cache.SetAsync(AnonymousSubCategoriesCacheKey,
                    JsonSerializer.SerializeToUtf8Bytes(anonymousSubCategories),
                    entryOptions));
        }

        if (context.Controller is Controller ctrl)
        {
            ctrl.ViewBag.AdminCategories = adminCategories;
            ctrl.ViewBag.AdminSubCategories = adminSubCategories;
            ctrl.ViewBag.UserCategories = userCategories;
            ctrl.ViewBag.UserSubCategories = userSubCategories;
            ctrl.ViewBag.AnonymousCategories = anonymousCategories;
            ctrl.ViewBag.AnonymousSubCategories = anonymousSubCategories;
        }

        var currentControllerName = context.ActionDescriptor.RouteValues["controller"] ?? ""; // Controller name before execution
        var currentActionName = context.ActionDescriptor.RouteValues["action"] ?? ""; // Action name before execution
        var currentRequestMethod = context.HttpContext.Request.Method; // GET HTTP METHOD
        var sessionAccount = _sessionService.GetAccount(); // Check if account session exists after login

        AccountResponse? loggedInAccount = null;
        if (sessionAccount != null)
        {
            loggedInAccount = await _accountService.GetAccountByEmailAsync(sessionAccount.Email ?? "", context.HttpContext.RequestAborted); // Re-check account state from DB so that Locked/Deleted/Role changes take effect on the next request, not only at login

            // SecurityStamp mismatch means the password changed (self-service, forgot-password
            // reset, or admin override) after this session was minted — the session was captured
            // by value at login, so it never sees the new stamp on its own.
            if (loggedInAccount == null || loggedInAccount.Deleted || loggedInAccount.Locked ||
                loggedInAccount.SecurityStamp != sessionAccount.SecurityStamp)
            {
                _sessionService.RemoveAccount(); // Invalidate this session immediately
                context.Result = new RedirectResult("/Account/Login");
                return;
            }

            // Hand the freshly re-validated account to ViewBagPopulatorFilter (runs next in the
            // pipeline) so it doesn't re-query the same row on every authenticated request.
            context.HttpContext.Items[HttpContextItemKeys.LoggedInAccount] = loggedInAccount;
        }

        var isAccountSessionExist = loggedInAccount != null;

        // An admin-forced password change locks the account to the profile page (where the
        // change-password form lives) and logout, until UpdateProfilePassword clears the flag.
        if (isAccountSessionExist && loggedInAccount!.MustChangePassword)
        {
            bool isAllowedWhileForced =
                (currentControllerName == "Management" && currentActionName is "Profile" or "UpdateProfilePassword") ||
                (currentControllerName == "Account" && currentActionName == "Logout") ||
                (currentControllerName == "Exception" && currentActionName == "Error");

            if (!isAllowedWhileForced)
            {
                context.Result = currentRequestMethod.ToUpper() == "GET"
                    ? new RedirectResult("/Management/Profile")
                    : new RedirectResult("/Dashboard/AnonymousIndex");
                return;
            }

            await next();
            return;
        }

        switch (currentRequestMethod.ToUpper())
        {
            // GET method (block page access only!!)
            // If not logged in
            case "GET" when !isAccountSessionExist:
                {
                    switch (currentControllerName)
                    {
                        // Allow exception resource access (Error)
                        case "Exception" when currentActionName == "Error":
                        // If login page
                        case "Account" when currentActionName == "Login":
                        // If registration page
                        case "Account" when currentActionName == "Register":
                        // If email confirmation page
                        case "Account" when currentActionName == "ConfirmEmail":
                        // If consent form page
                        case "Account" when currentActionName == "ConsentForm":
                        // If forgot password page
                        case "Account" when currentActionName == "ForgotPassword":
                        // If password reset page
                        case "Account" when currentActionName == "ResetPassword":
                            await next(); // Do Action!!
                            return;
                    }

                    var anonymousMatch = anonymousCategories.ResolveActiveMenuItem(
                        anonymousSubCategories, currentControllerName, currentActionName);

                    if (anonymousMatch.IsMatch)
                    {
                        await next(); // Do Action!!
                        return;
                    }

                    context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                    return;
                }
            case "GET" when isAccountSessionExist:
                {
                    switch (currentControllerName)
                    {
                        // Allow exception resource access (Error)
                        case "Exception" when currentActionName == "Error":
                        // Initial screen
                        case "Dashboard" when currentActionName == "AnonymousIndex":
                            await next(); // Do Action!!
                            return;
                    }

                    switch (loggedInAccount!.Role)
                    {
                        // Admin and User share identical resolution logic — only which
                        // category/sub-category list is searched differs by role.
                        case Role.Admin:
                        case Role.User:
                            {
                                bool isAdmin = loggedInAccount.Role == Role.Admin;
                                var match = (isAdmin ? adminCategories : userCategories).ResolveActiveMenuItem(
                                    isAdmin ? adminSubCategories : userSubCategories, currentControllerName, currentActionName);

                                if (match.IsMatch)
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                            }
                        // Role is a plain string, not an enum — an account somehow persisted with
                        // neither Admin nor User must still get an explicit result instead of
                        // falling through with next() never called and context.Result never set,
                        // which would silently short-circuit the pipeline with an empty response.
                        default:
                            context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                            return;
                    }
                }
            // POST method — not logged in
            case "POST" when !isAccountSessionExist:
                {
                    (string, string) endpoint = (currentControllerName, currentActionName);

                    // AccountController's own POST actions (login, register, forgot/reset password …)
                    // are always reachable to an anonymous visitor.
                    if (AnonymousAccountPostActions.Contains(endpoint))
                    {
                        await next(); // Do Action!!
                        return;
                    }

                    // Otherwise the action must be explicitly marked for the Anonymous role AND live
                    // on a controller that appears in the anonymous navigation menu.
                    if (PostActionRoles.TryGetValue(endpoint, out IReadOnlySet<string>? anonRoles)
                        && anonRoles.Contains(Role.Anonymous)
                        && anonymousCategories.Any(category => category.Controller == currentControllerName))
                    {
                        await next(); // Do Action!!
                        return;
                    }

                    context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                    return;
                }
            // POST method — logged in
            case "POST" when isAccountSessionExist:
                {
                    // Logout is a POST (antiforgery-protected) endpoint on AccountController, which is
                    // not part of any role's navigation menu — allow it explicitly for any signed-in
                    // account regardless of role.
                    if (currentControllerName == "Account" && currentActionName == "Logout")
                    {
                        await next();
                        return;
                    }

                    switch (loggedInAccount!.Role)
                    {
                        // Admin and User share identical resolution logic — only which category
                        // list is searched for a controller with a matching RequiredHttpPostAccess
                        // role differs.
                        case Role.Admin:
                        case Role.User:
                            {
                                var categories = loggedInAccount.Role == Role.Admin ? adminCategories : userCategories;

                                // The action must carry [RequiredHttpPostAccess(Role = <this role>)]
                                // AND its controller must appear in this role's navigation menu.
                                if (PostActionRoles.TryGetValue((currentControllerName, currentActionName), out IReadOnlySet<string>? allowedRoles)
                                    && allowedRoles.Contains(loggedInAccount.Role)
                                    && categories.Any(category => category.Controller == currentControllerName))
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                            }
                        // Role is a plain string, not an enum — an account somehow persisted with
                        // neither Admin nor User must still get an explicit result instead of
                        // falling through with next() never called and context.Result never set,
                        // which would silently short-circuit the pipeline with an empty response.
                        default:
                            context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                            return;
                    }
                }
            // (Reject all methods like PATCH / DELETE etc.)
            default:
                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                return;
        }
    }
}
