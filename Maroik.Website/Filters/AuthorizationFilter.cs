using System.Reflection;
using System.Text.Json;
using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Attributes;
using Maroik.Website.Constants;
using Maroik.Website.Contracts;
using Maroik.Website.Mappings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Maroik.Website.Filters;

/// <summary>
/// Action filter that loads and caches the navigation menu (categories and sub-categories)
/// for all three roles (Admin, User, Anonymous) and sets them on ViewBag before each action.
/// The menu data is cached with a 10-minute sliding expiration to reduce database load.
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
        Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => typeof(Controller).IsAssignableFrom(type))
            .ToList();

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

        IEnumerable<CategoryResponse> adminCategories;
        IEnumerable<SubCategoryResponse> adminSubCategories;
        IEnumerable<CategoryResponse> userCategories;
        IEnumerable<SubCategoryResponse> userSubCategories;
        IEnumerable<CategoryResponse> anonymousCategories;
        IEnumerable<SubCategoryResponse> anonymousSubCategories;

        if (results[0] == null || results[1] == null || results[2] == null ||
            results[3] == null || results[4] == null || results[5] == null)
        {
            var categories = (await _menuService.GetAllCategoriesAsync()).ToList();
            var subCategories = (await _menuService.GetAllSubCategoriesAsync()).ToList();

            var entryOptions = new DistributedCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            adminCategories = categories.Where(c => c.Role == Role.Admin).ToList();
            adminSubCategories = subCategories.Where(s => s.Role == Role.Admin).ToList();
            userCategories = categories.Where(c => c.Role == Role.User).ToList();
            userSubCategories = subCategories.Where(s => s.Role == Role.User).ToList();
            anonymousCategories = categories.Where(c => c.Role == Role.Anonymous).ToList();
            anonymousSubCategories = subCategories.Where(s => s.Role == Role.Anonymous).ToList();

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
        else
        {
            adminCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[0]!)!;
            adminSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[1]!)!;
            userCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[2]!)!;
            userSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[3]!)!;
            anonymousCategories = JsonSerializer.Deserialize<List<CategoryResponse>>(results[4]!)!;
            anonymousSubCategories = JsonSerializer.Deserialize<List<SubCategoryResponse>>(results[5]!)!;
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

            if (loggedInAccount == null || loggedInAccount.Deleted || loggedInAccount.Locked)
            {
                _sessionService.RemoveAccount(); // Invalidate this session immediately
                context.Result = new RedirectResult("/Account/Login");
                return;
            }
        }

        var isAccountSessionExist = loggedInAccount != null;

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
                        // If logout
                        case "Account" when currentActionName == "Logout":
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

                    //context.Result = new UnauthorizedObjectResult("User is unauthorized");
                    context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                    return;
                }
            case "GET" when isAccountSessionExist:
                {
                    switch (currentControllerName)
                    {
                        // Allow exception resource access (Error)
                        case "Exception" when currentActionName == "Error":
                        // Logout
                        case "Account" when currentActionName == "Logout":
                        // Initial screen
                        case "Dashboard" when currentActionName == "AnonymousIndex":
                            await next(); // Do Action!!
                            return;
                    }

                    switch (loggedInAccount!.Role)
                    {
                        // If logged-in user has Admin role
                        case Role.Admin:
                            {
                                var adminMatch = adminCategories.ResolveActiveMenuItem(
                                    adminSubCategories, currentControllerName, currentActionName);

                                if (adminMatch.IsMatch)
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                //context.Result = new UnauthorizedObjectResult("User is unauthorized"); // If not found after all checks, it doesn't exist in DB
                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                                //Extract all DB Categories & SubCategories, compare with current accessed Controller & Action,
                                //proceed if match or output UnauthorizedObjectResult (Cancel Action!!)
                            }
                        // If logged-in user has User role
                        case Role.User:
                            {
                                var userMatch = userCategories.ResolveActiveMenuItem(
                                    userSubCategories, currentControllerName, currentActionName);

                                if (userMatch.IsMatch)
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                //context.Result = new UnauthorizedObjectResult("User is unauthorized"); // If not found after all checks, it doesn't exist in DB
                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                                //Extract all DB Categories & SubCategories, compare with current accessed Controller & Action,
                                //proceed if match or output UnauthorizedObjectResult (Cancel Action!!)
                            }
                    }

                    break;
                }
            // POST method — not logged in
            case "POST" when !isAccountSessionExist:
                {
                    var existingControllers = _cachedControllerTypes;

                    var existingHttpPostMethods = existingControllers
                        .Where(type => type.Name == "AccountController") // Check AccountController
                        .SelectMany(type => type.GetMethods()) // Get methods
                        .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) &&
                                         method.IsDefined(typeof(HttpPostAttribute)) &&
                                         method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)));
                    // Get methods with public access modifier, without NonAction attribute, with HttpPost and ValidateAntiForgeryToken attributes

                    if (existingHttpPostMethods.Any(existingHttpPostMethod =>
                            currentControllerName ==
                            existingHttpPostMethod.DeclaringType?.Name.Replace("Controller", "") &&
                            currentActionName == existingHttpPostMethod.Name))
                    {
                        await next(); // Do Action!!
                        return;
                    }

                    foreach (var anonymousCategory in anonymousCategories ?? [])
                    {
                        existingHttpPostMethods = existingControllers
                            .Where(type =>
                                type.Name ==
                                $"{anonymousCategory.Controller}Controller") // When permission is AnonymousCategory, only registered Category's Controller name is accessible
                            .SelectMany(type => type.GetMethods()) // Get methods
                            .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) &&
                                             method.IsDefined(typeof(HttpPostAttribute)) &&
                                             method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) &&
                                             method.IsDefined(typeof(RequiredHttpPostAccessAttribute)));
                        // Get methods with public access modifier, without NonAction attribute, with HttpPost, ValidateAntiForgeryToken, and RequiredHttpPostAccess attributes

                        if (!(from existingHttpPostMethod in existingHttpPostMethods
                              where currentControllerName ==
                                    existingHttpPostMethod.DeclaringType?.Name.Replace("Controller", "") &&
                                    currentActionName == existingHttpPostMethod.Name
                              from customAttribute in existingHttpPostMethod.CustomAttributes
                              where customAttribute.AttributeType.FullName ==
                                    typeof(RequiredHttpPostAccessAttribute).ToString()
                              from namedArgument in customAttribute.NamedArguments
                              select namedArgument).Any(namedArgument =>
                              (namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) &&
                              ((namedArgument.TypedValue.Value as string) == Role.Anonymous)))
                        {
                            continue;
                        }

                        await next(); // Do Action!!
                        return;
                    }

                    //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                    context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                    return;
                }
            // POST method — logged in
            case "POST" when isAccountSessionExist:
                {
                    var existingControllers = _cachedControllerTypes;

                    switch (loggedInAccount!.Role)
                    {
                        // If logged-in user has Admin role
                        case Role.Admin:
                            {
                                if ((from adminCategory in adminCategories ?? []
                                     from existingHttpPostMethod in existingControllers
                                         .Where(type =>
                                             type.Name ==
                                             $"{adminCategory.Controller}Controller") // When permission is Admin, only registered Category's Controller name is accessible
                                         .SelectMany(type => type.GetMethods()) // Get methods
                                         .Where(method =>
                                             method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) &&
                                             method.IsDefined(typeof(HttpPostAttribute)) &&
                                             method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) &&
                                             method.IsDefined(typeof(RequiredHttpPostAccessAttribute)))
                                     where
                                         currentControllerName ==
                                         existingHttpPostMethod.DeclaringType?.Name.Replace("Controller", "") &&
                                         currentActionName == existingHttpPostMethod.Name
                                     from customAttribute in existingHttpPostMethod.CustomAttributes
                                     where customAttribute.AttributeType.FullName ==
                                           typeof(RequiredHttpPostAccessAttribute).ToString()
                                     from namedArgument in customAttribute.NamedArguments
                                     select namedArgument).Any(namedArgument =>
                                     (namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) &&
                                     ((namedArgument.TypedValue.Value as string) == loggedInAccount.Role)))
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                            }
                        // If logged-in user has User role
                        case Role.User:
                            {
                                if ((from userCategory in userCategories ?? []
                                     from existingHttpPostMethod in existingControllers
                                         .Where(type =>
                                             type.Name ==
                                             $"{userCategory.Controller}Controller") // When permission is User, only registered Category's Controller name is accessible
                                         .SelectMany(type => type.GetMethods()) // Get methods
                                         .Where(method =>
                                             method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) &&
                                             method.IsDefined(typeof(HttpPostAttribute)) &&
                                             method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) &&
                                             method.IsDefined(typeof(RequiredHttpPostAccessAttribute)))
                                     where
                                         currentControllerName ==
                                         existingHttpPostMethod.DeclaringType?.Name.Replace("Controller", "") &&
                                         currentActionName == existingHttpPostMethod.Name
                                     from customAttribute in existingHttpPostMethod.CustomAttributes
                                     where customAttribute.AttributeType.FullName ==
                                           typeof(RequiredHttpPostAccessAttribute).ToString()
                                     from namedArgument in customAttribute.NamedArguments
                                     select namedArgument).Any(namedArgument =>
                                     (namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) &&
                                     ((namedArgument.TypedValue.Value as string) == loggedInAccount.Role)))
                                {
                                    await next(); // Do Action!!
                                    return;
                                }

                                //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                                return;
                            }
                    }

                    break;
                }
            // (Reject all methods like PATCH / DELETE etc.)
            default:
                //await next(); // Do Action!!
                //context.Result = new UnauthorizedObjectResult("Access is not allowed [Only Allowed GET/POST Method]");
                context.Result = new RedirectResult("/Dashboard/AnonymousIndex");
                return;
        }
    }
}
