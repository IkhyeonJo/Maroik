using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

namespace Maroik.WebSite.Utilities;

public class AuthorizationFilter : ActionFilterAttribute
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;

    public AuthorizationFilter(ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
    }

    // Do something before the action executes.
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var categories = await _categoryRepository.GetCategoryAsync();
        var subCategories = await _subCategoryRepository.GetSubCategoryAsync();

        DbCache.AdminCategories = from adminCategory in categories
                                  where adminCategory.Role == Role.Admin
                                  select adminCategory;

        DbCache.AdminSubCategories = from adminSubCategory in subCategories
                                     where adminSubCategory.Role == Role.Admin
                                     select adminSubCategory;

        DbCache.UserCategories = from userCategory in categories
                                 where userCategory.Role == Role.User
                                 select userCategory;

        DbCache.UserSubCategories = from userSubCategory in subCategories
                                    where userSubCategory.Role == Role.User
                                    select userSubCategory;

        DbCache.AnonymousCategories = from anonymousCategory in categories
                                      where anonymousCategory.Role == Role.Anonymous
                                      select anonymousCategory;

        DbCache.AnonymousSubCategories = from anonymousSubCategory in subCategories
                                         where anonymousSubCategory.Role == Role.Anonymous
                                         select anonymousSubCategory;


        string currentControllerName = context.ActionDescriptor.RouteValues["controller"].ToString(); // Controller name before execution
        string currentActionName = context.ActionDescriptor.RouteValues["action"].ToString(); // Action name before execution
        string currentRequestMethod = context.HttpContext.Request.Method; // GET HTTP METHOD
        bool isAccountSessionExist = context.HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out _); // Check if account session exists after login

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

                IEnumerable<Category> anonymousCategories = DbCache.AnonymousCategories;
                IEnumerable<SubCategory> anonymousSubCategories = DbCache.AnonymousSubCategories;

                #region If Category without SubCategory
                IEnumerable<Category> singleCategories = from anonymousCategory in anonymousCategories
                                                         where !string.IsNullOrEmpty(anonymousCategory.Action)
                                                         select anonymousCategory; // Collection of categories without sub-categories

                foreach (Category singleCategory in singleCategories)
                {
                    if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                    {
                        await next(); // Do Action!!
                        return;
                    }
                }
                #endregion

                #region If Category with SubCategory
                IEnumerable<Category> nonSingleCategories = from anonymousCategory in anonymousCategories
                                                            where string.IsNullOrEmpty(anonymousCategory.Action)
                                                            select anonymousCategory; // Collection of categories with sub-categories

                foreach (Category nonSingleCategory in nonSingleCategories)
                {
                    IEnumerable<SubCategory> nonSingleCategorysSubCategories = from anonymousSubCategory in anonymousSubCategories
                                                                               where anonymousSubCategory.CategoryId == nonSingleCategory.Id
                                                                               select anonymousSubCategory;
                    foreach (SubCategory nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
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

                _ = context.HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                if (loginedAccount.Role == Role.Admin) // If logged-in user has Admin role
                {
                    IEnumerable<Category> adminCategories = DbCache.AdminCategories;
                    IEnumerable<SubCategory> adminSubCategories = DbCache.AdminSubCategories;

                    #region If Category without SubCategory
                    IEnumerable<Category> singleCategories = from adminCategory in adminCategories
                                                             where !string.IsNullOrEmpty(adminCategory.Action)
                                                             select adminCategory; // Collection of categories without sub-categories

                    foreach (Category singleCategory in singleCategories)
                    {
                        if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                        {
                            await next(); // Do Action!!
                            return;
                        }
                    }
                    #endregion

                    #region If Category with SubCategory
                    IEnumerable<Category> nonSingleCategories = from adminCategory in adminCategories
                                                                where string.IsNullOrEmpty(adminCategory.Action)
                                                                select adminCategory; // Collection of categories with sub-categories

                    foreach (Category nonSingleCategory in nonSingleCategories)
                    {
                        IEnumerable<SubCategory> nonSingleCategorysSubCategories = from adminSubCategory in adminSubCategories
                                                                                   where adminSubCategory.CategoryId == nonSingleCategory.Id
                                                                                   select adminSubCategory;
                        foreach (SubCategory nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
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
                    IEnumerable<Category> userCategories = DbCache.UserCategories;
                    IEnumerable<SubCategory> userSubCategories = DbCache.UserSubCategories;

                    #region If Category without SubCategory
                    IEnumerable<Category> singleCategories = from userCategory in userCategories
                                                             where !string.IsNullOrEmpty(userCategory.Action)
                                                             select userCategory; // Collection of categories without sub-categories

                    foreach (Category singleCategory in singleCategories)
                    {
                        if (singleCategory.Controller == currentControllerName && singleCategory.Action == currentActionName)
                        {
                            await next(); // Do Action!!
                            return;
                        }
                    }
                    #endregion

                    #region If Category with SubCategory
                    IEnumerable<Category> nonSingleCategories = from userCategory in userCategories
                                                                where string.IsNullOrEmpty(userCategory.Action)
                                                                select userCategory; // Collection of categories with sub-categories

                    foreach (Category nonSingleCategory in nonSingleCategories)
                    {
                        IEnumerable<SubCategory> nonSingleCategorysSubCategories = from userSubCategory in userSubCategories
                                                                                   where userSubCategory.CategoryId == nonSingleCategory.Id
                                                                                   select userSubCategory;
                        foreach (SubCategory nonSingleCategorysSubCategory in nonSingleCategorysSubCategories)
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

                IEnumerable<Category> anonymousCategories = DbCache.AnonymousCategories;
                foreach (Category anonymousCategory in anonymousCategories)
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
                _ = context.HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                if (loginedAccount.Role == Role.Admin) // If logged-in user has Admin role
                {
                    IEnumerable<Category> adminCategories = DbCache.AdminCategories;
                    foreach (Category adminCategory in adminCategories)
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
                    IEnumerable<Category> userCategories = DbCache.UserCategories;
                    foreach (Category userCategory in userCategories)
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