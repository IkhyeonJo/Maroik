using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Maroik.WebSite.Utilities;

public class AuthorizationFilter : ActionFilterAttribute
{
    private readonly IMenuService _menuService;
    private readonly IMemoryCache _cache;
    private readonly ISessionService _sessionService;

    private const string AdminCategoriesCacheKey = "nav_admin_categories";
    private const string AdminSubCategoriesCacheKey = "nav_admin_subcategories";
    private const string UserCategoriesCacheKey = "nav_user_categories";
    private const string UserSubCategoriesCacheKey = "nav_user_subcategories";
    private const string AnonymousCategoriesCacheKey = "nav_anonymous_categories";
    private const string AnonymousSubCategoriesCacheKey = "nav_anonymous_subcategories";

    public AuthorizationFilter(IMenuService menuService, IMemoryCache cache, ISessionService sessionService)
    {
        _menuService = menuService;
        _cache = cache;
        _sessionService = sessionService;
    }

    // Do something before the action executes.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!_cache.TryGetValue(AdminCategoriesCacheKey, out IEnumerable<CategoryResponse> adminCategories) ||
            !_cache.TryGetValue(AdminSubCategoriesCacheKey, out IEnumerable<SubCategoryResponse> adminSubCategories) ||
            !_cache.TryGetValue(UserCategoriesCacheKey, out IEnumerable<CategoryResponse> userCategories) ||
            !_cache.TryGetValue(UserSubCategoriesCacheKey, out IEnumerable<SubCategoryResponse> userSubCategories) ||
            !_cache.TryGetValue(AnonymousCategoriesCacheKey, out IEnumerable<CategoryResponse> anonymousCategories) ||
            !_cache.TryGetValue(AnonymousSubCategoriesCacheKey, out IEnumerable<SubCategoryResponse> anonymousSubCategories))
        {
            var categories = (await _menuService.GetAllCategoriesAsync()).ToList();
            var subCategories = (await _menuService.GetAllSubCategoriesAsync()).ToList();

            var entryOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10));

            adminCategories = categories.Where(c => c.Role == Role.Admin).ToList();
            adminSubCategories = subCategories.Where(s => s.Role == Role.Admin).ToList();
            userCategories = categories.Where(c => c.Role == Role.User).ToList();
            userSubCategories = subCategories.Where(s => s.Role == Role.User).ToList();
            anonymousCategories = categories.Where(c => c.Role == Role.Anonymous).ToList();
            anonymousSubCategories = subCategories.Where(s => s.Role == Role.Anonymous).ToList();

            _cache.Set(AdminCategoriesCacheKey, adminCategories, entryOptions);
            _cache.Set(AdminSubCategoriesCacheKey, adminSubCategories, entryOptions);
            _cache.Set(UserCategoriesCacheKey, userCategories, entryOptions);
            _cache.Set(UserSubCategoriesCacheKey, userSubCategories, entryOptions);
            _cache.Set(AnonymousCategoriesCacheKey, anonymousCategories, entryOptions);
            _cache.Set(AnonymousSubCategoriesCacheKey, anonymousSubCategories, entryOptions);
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

        string currentControllerName = context.ActionDescriptor.RouteValues["controller"].ToString(); // Controller name before execution
        string currentActionName = context.ActionDescriptor.RouteValues["action"].ToString(); // Action name before execution
        string currentRequestMethod = context.HttpContext.Request.Method; // GET HTTP METHOD
        bool isAccountSessionExist = _sessionService.GetAccount() != null; // Check if account session exists after login

        if (currentRequestMethod.ToUpper() == "GET") // GET method (block page access only!!)
        {
            if (isAccountSessionExist == false) // If not logged in
            {
                if (currentControllerName == "Exception" && currentActionName == "Error") // Allow exception resource access (Error)
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "Login") // If login page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "Register") // If registration page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "ConfirmEmail") // If email confirmation page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "ConsentForm") // If consent form page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "ForgotPassword") // If forgot password page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "ResetPassword") // If password reset page
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "Logout") // If logout
                {
                    await next(); // Do Action!!
                    return;
                }

                #region If Category without SubCategory
                IEnumerable<CategoryResponse> singleCategories = from anonymousCategory in anonymousCategories
                                                         where !string.IsNullOrEmpty(anonymousCategory.Action)
                                                         select anonymousCategory; // Collection of categories without sub-categories

                foreach (CategoryResponse singleCategory in singleCategories)
                {
                    if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                    {
                        await next(); // Do Action!!
                        return;
                    }
                }
                #endregion

                #region If Category with SubCategory
                IEnumerable<CategoryResponse> nonSingleCategories = from anonymousCategory in anonymousCategories
                                                            where string.IsNullOrEmpty(anonymousCategory.Action)
                                                            select anonymousCategory; // Collection of categories with sub-categories

                foreach (CategoryResponse nonSingleCategory in nonSingleCategories)
                {
                    IEnumerable<SubCategoryResponse> nonSingleCategorysSubCategories = from anonymousSubCategory in anonymousSubCategories
                                                                               where anonymousSubCategory.CategoryId == nonSingleCategory.Id
                                                                               select anonymousSubCategory;
                    foreach (SubCategoryResponse nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
                    {
                        if (nonSingleCategory.Controller == currentControllerName && nonSingleCategorysSubCategory.Action == currentActionName)
                        {
                            await next(); // Do Action!!
                            return;
                        }
                    }
                }
                #endregion
                //context.Result = new UnauthorizedObjectResult("User is unauthorized");
                context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                return;
            }
            else if (isAccountSessionExist == true) // If logged in
            {
                if (currentControllerName == "Exception" && currentActionName == "Error") // Allow exception resource access (Error)
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "Account" && currentActionName == "Logout") // Logout
                {
                    await next(); // Do Action!!
                    return;
                }
                else if (currentControllerName == "DashBoard" && currentActionName == "AnonymousIndex") // Initial screen
                {
                    await next(); // Do Action!!
                    return;
                }

                AccountResponse loginedAccount = _sessionService.GetAccount()!; // Get Session

                if (loginedAccount.Role == Role.Admin) // If logged-in user has Admin role
                {
                    #region If Category without SubCategory
                    IEnumerable<CategoryResponse> singleCategories = from adminCategory in adminCategories
                                                             where !string.IsNullOrEmpty(adminCategory.Action)
                                                             select adminCategory; // Collection of categories without sub-categories

                    foreach (CategoryResponse singleCategory in singleCategories)
                    {
                        if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                        {
                            await next(); // Do Action!!
                            return;
                        }
                    }
                    #endregion

                    #region If Category with SubCategory
                    IEnumerable<CategoryResponse> nonSingleCategories = from adminCategory in adminCategories
                                                                where string.IsNullOrEmpty(adminCategory.Action)
                                                                select adminCategory; // Collection of categories with sub-categories

                    foreach (CategoryResponse nonSingleCategory in nonSingleCategories)
                    {
                        IEnumerable<SubCategoryResponse> nonSingleCategorysSubCategories = from adminSubCategory in adminSubCategories
                                                                                   where adminSubCategory.CategoryId == nonSingleCategory.Id
                                                                                   select adminSubCategory;
                        foreach (SubCategoryResponse nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
                        {
                            if (nonSingleCategory.Controller == currentControllerName && nonSingleCategorysSubCategory.Action == currentActionName)
                            {
                                await next(); // Do Action!!
                                return;
                            }
                        }
                    }
                    #endregion
                    //context.Result = new UnauthorizedObjectResult("User is unauthorized"); // If not found after all checks, it doesn't exist in DB
                    context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                    return;
                    //Extract all DB Categories & SubCategories, compare with current accessed Controller & Action, proceed if match or output UnauthorizedObjectResult (Cancel Action!!)
                }
                else if (loginedAccount.Role == Role.User) // If logged-in user has User role
                {
                    #region If Category without SubCategory
                    IEnumerable<CategoryResponse> singleCategories = from userCategory in userCategories
                                                             where !string.IsNullOrEmpty(userCategory.Action)
                                                             select userCategory; // Collection of categories without sub-categories

                    foreach (CategoryResponse singleCategory in singleCategories)
                    {
                        if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                        {
                            await next(); // Do Action!!
                            return;
                        }
                    }
                    #endregion

                    #region If Category with SubCategory
                    IEnumerable<CategoryResponse> nonSingleCategories = from userCategory in userCategories
                                                                where string.IsNullOrEmpty(userCategory.Action)
                                                                select userCategory; // Collection of categories with sub-categories

                    foreach (CategoryResponse nonSingleCategory in nonSingleCategories)
                    {
                        IEnumerable<SubCategoryResponse> nonSingleCategorysSubCategories = from userSubCategory in userSubCategories
                                                                                   where userSubCategory.CategoryId == nonSingleCategory.Id
                                                                                   select userSubCategory;
                        foreach (SubCategoryResponse nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
                        {
                            if (nonSingleCategory.Controller == currentControllerName && nonSingleCategorysSubCategory.Action == currentActionName)
                            {
                                await next(); // Do Action!!
                                return;
                            }
                        }
                    }
                    #endregion
                    //context.Result = new UnauthorizedObjectResult("User is unauthorized"); // If not found after all checks, it doesn't exist in DB
                    context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                    return;
                    //Extract all DB Categories & SubCategories, compare with current accessed Controller & Action, proceed if match or output UnauthorizedObjectResult (Cancel Action!!)
                }
            }
        }
        else if (currentRequestMethod.ToUpper() == "POST") // POST method
        {
            IEnumerable<Type> existingControllers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => typeof(Controller).IsAssignableFrom(type)); //filter controllers

            if (isAccountSessionExist == false) // If not logged in
            {
                IEnumerable<MethodInfo> existingHttpPostMethods = existingControllers
                .Where(type => type.Name == "AccountController") // Check AccountController
                .SelectMany(type => type.GetMethods()) // Get methods
                .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute))); // Get methods with public access modifier, without NonAction attribute, with HttpPost and ValidateAntiForgeryToken attributes

                foreach (MethodInfo existingHttpPostMethod in existingHttpPostMethods)
                {
                    if (currentControllerName == existingHttpPostMethod.DeclaringType.Name.Replace("Controller", "") && currentActionName == existingHttpPostMethod.Name) // If current Controller and Action names (sent via POST) match Account Controller's Method(Action) name without NonAction attribute and with HttpPost & ValidateAntiForgeryToken attributes
                    {
                        await next(); // Do Action!!
                        return;
                    }
                }

                foreach (CategoryResponse anonymousCategory in anonymousCategories)
                {
                    existingHttpPostMethods = existingControllers
                    .Where(type => type.Name == $"{anonymousCategory.Controller}Controller") // When permission is AnonymousCategory, only registered Category's Controller name is accessible
                    .SelectMany(type => type.GetMethods()) // Get methods
                    .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) && method.IsDefined(typeof(RequiredHttpPostAccessAttribute))); // Get methods with public access modifier, without NonAction attribute, with HttpPost, ValidateAntiForgeryToken, and RequiredHttpPostAccess attributes

                    foreach (MethodInfo existingHttpPostMethod in existingHttpPostMethods)
                    {
                        if (currentControllerName == existingHttpPostMethod.DeclaringType.Name.Replace("Controller", "") && currentActionName == existingHttpPostMethod.Name) // If current Controller and Action names (sent via POST) match Account Controller's Method(Action) name without NonAction attribute and with HttpPost & ValidateAntiForgeryToken attributes
                        {
                            foreach (CustomAttributeData customAttribute in existingHttpPostMethod.CustomAttributes)
                            {
                                if (customAttribute.AttributeType.FullName == typeof(RequiredHttpPostAccessAttribute).ToString())
                                {
                                    foreach (CustomAttributeNamedArgument namedArgument in customAttribute.NamedArguments)
                                    {
                                        if ((namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) && ((namedArgument.TypedValue.Value as string) == Role.Anonymous))
                                        {
                                            await next(); // Do Action!!
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                return;
            }
            else if (isAccountSessionExist == true) // If logged in
            {
                AccountResponse loginedAccount = _sessionService.GetAccount()!; // Get Session

                if (loginedAccount.Role == Role.Admin) // If logged-in user has Admin role
                {
                    foreach (CategoryResponse adminCategory in adminCategories)
                    {
                        IEnumerable<MethodInfo> existingHttpPostMethods = existingControllers
                        .Where(type => type.Name == $"{adminCategory.Controller}Controller") // When permission is Admin, only registered Category's Controller name is accessible
                        .SelectMany(type => type.GetMethods()) // Get methods
                        .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) && method.IsDefined(typeof(RequiredHttpPostAccessAttribute))); // Get methods with public access modifier, without NonAction attribute, with HttpPost, ValidateAntiForgeryToken, and RequiredHttpPostAccess attributes

                        foreach (MethodInfo existingHttpPostMethod in existingHttpPostMethods)
                        {
                            if (currentControllerName == existingHttpPostMethod.DeclaringType.Name.Replace("Controller", "") && currentActionName == existingHttpPostMethod.Name) // If current Controller and Action names (sent via POST) match Account Controller's Method(Action) name without NonAction attribute and with HttpPost & ValidateAntiForgeryToken attributes
                            {
                                foreach (CustomAttributeData customAttribute in existingHttpPostMethod.CustomAttributes)
                                {
                                    if (customAttribute.AttributeType.FullName == typeof(RequiredHttpPostAccessAttribute).ToString())
                                    {
                                        foreach (CustomAttributeNamedArgument namedArgument in customAttribute.NamedArguments)
                                        {
                                            if ((namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) && ((namedArgument.TypedValue.Value as string) == loginedAccount.Role))
                                            {
                                                await next(); // Do Action!!
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                    context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                    return;
                }
                else if (loginedAccount.Role == Role.User) // If logged-in user has User role
                {
                    foreach (CategoryResponse userCategory in userCategories)
                    {
                        IEnumerable<MethodInfo> existingHttpPostMethods = existingControllers
                        .Where(type => type.Name == $"{userCategory.Controller}Controller") // When permission is User, only registered Category's Controller name is accessible
                        .SelectMany(type => type.GetMethods()) // Get methods
                        .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)) && method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) && method.IsDefined(typeof(RequiredHttpPostAccessAttribute))); // Get methods with public access modifier, without NonAction attribute, with HttpPost, ValidateAntiForgeryToken, and RequiredHttpPostAccess attributes

                        foreach (MethodInfo existingHttpPostMethod in existingHttpPostMethods)
                        {
                            if (currentControllerName == existingHttpPostMethod.DeclaringType.Name.Replace("Controller", "") && currentActionName == existingHttpPostMethod.Name) // If current Controller and Action names (sent via POST) match Account Controller's Method(Action) name without NonAction attribute and with HttpPost & ValidateAntiForgeryToken attributes
                            {
                                foreach (CustomAttributeData customAttribute in existingHttpPostMethod.CustomAttributes)
                                {
                                    if (customAttribute.AttributeType.FullName == typeof(RequiredHttpPostAccessAttribute).ToString())
                                    {
                                        foreach (CustomAttributeNamedArgument namedArgument in customAttribute.NamedArguments)
                                        {
                                            if ((namedArgument.MemberName == nameof(RequiredHttpPostAccessAttribute.Role)) && ((namedArgument.TypedValue.Value as string) == loginedAccount.Role))
                                            {
                                                await next(); // Do Action!!
                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //context.Result = new UnauthorizedObjectResult("Access is not allowed"); // If not found after all checks, execution is not allowed!
                    context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
                    return;
                }
            }
        }
        else // (Reject all methods like PATCH / DELETE etc.)
        {
            //await next(); // Do Action!!
            //context.Result = new UnauthorizedObjectResult("Access is not allowed [Only Allowed GET/POST Method]");
            context.Result = new RedirectResult("/DashBoard/AnonymousIndex");
            return;
        }
    }
}
