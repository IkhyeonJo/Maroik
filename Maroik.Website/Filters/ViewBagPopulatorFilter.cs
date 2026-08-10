using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Website.Contracts;
using Maroik.Website.Mappings;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Filters;

/// <summary>
/// Global action filter that populates shared ViewBag entries before every action executes:
/// domain name, file-size limit, logged-in account, the active navigation menu item (for
/// sidebar highlighting/breadcrumbs), notification badge counts, current culture, and the
/// return URI. Runs after <see cref="AuthorizationFilter"/>, which has already published
/// ViewBag.{Role}Categories/{Role}SubCategories and decided allow/deny for this request.
/// </summary>
public class ViewBagPopulatorFilter : IAsyncActionFilter
{
    private readonly IAccountService _accountService;
    private readonly IDashboardService _dashboardService;
    private readonly ITimeZoneCatalogService _timeZoneCatalogService;
    private readonly IOptions<ServerSetting> _serverSettings;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="ViewBagPopulatorFilter"/> with the supplied dependencies.</summary>
    public ViewBagPopulatorFilter(
        IAccountService accountService,
        IDashboardService dashboardService,
        ITimeZoneCatalogService timeZoneCatalogService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
    {
        _accountService = accountService;
        _dashboardService = dashboardService;
        _timeZoneCatalogService = timeZoneCatalogService;
        _serverSettings = serverSettings;
        _sessionService = sessionService;
    }

    /// <summary>Populates shared ViewBag entries before each action executes.</summary>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>().CreateLogger<ViewBagPopulatorFilter>();

        if (context.Controller is not Controller controller)
        {
            await next();
            return;
        }

        controller.ViewBag.DomainName = _serverSettings.Value.DomainName;
        controller.ViewBag.MaxAttachedFileSizeBytes = _serverSettings.Value.MaxAttachedFileSizeBytes;

        AccountResponse loggedInAccount = new()
        {
            Nickname = "Login",
            Role = Role.Anonymous,
            TimeZoneIanaId = "UTC",
            AvatarImagePath = "/anonymous/images/bg1.jpg"
        };

        var sessionAccount = _sessionService.GetAccount();
        if (sessionAccount != null)
        {
            try
            {
                string email = sessionAccount.Email ?? "";
                if (!string.IsNullOrEmpty(email))
                    loggedInAccount = await _accountService.GetAccountByEmailAsync(email, context.HttpContext.RequestAborted) ?? loggedInAccount;
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to refresh session account for {Email}", sessionAccount.Email); }
        }

        controller.ViewBag.LoggedInAccount = loggedInAccount;
        controller.ViewBag.TimeZoneOptions = _timeZoneCatalogService.GetTimeZoneOptions();
        controller.ViewBag.CopyrightYear = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loggedInAccount.TimeZoneIanaId ?? "UTC").Year;

        (IEnumerable<CategoryResponse>? roleCategories, IEnumerable<SubCategoryResponse>? roleSubCategories) = loggedInAccount.Role switch
        {
            Role.Admin => ((IEnumerable<CategoryResponse>?)controller.ViewBag.AdminCategories, (IEnumerable<SubCategoryResponse>?)controller.ViewBag.AdminSubCategories),
            Role.User => ((IEnumerable<CategoryResponse>?)controller.ViewBag.UserCategories, (IEnumerable<SubCategoryResponse>?)controller.ViewBag.UserSubCategories),
            _ => ((IEnumerable<CategoryResponse>?)controller.ViewBag.AnonymousCategories, (IEnumerable<SubCategoryResponse>?)controller.ViewBag.AnonymousSubCategories)
        };

        if (roleCategories != null && roleSubCategories != null)
        {
            context.ActionDescriptor.RouteValues.TryGetValue("controller", out var currentControllerName);
            context.ActionDescriptor.RouteValues.TryGetValue("action", out var currentActionName);
            var activeMenuMatch = roleCategories.ResolveActiveMenuItem(roleSubCategories, currentControllerName ?? "", currentActionName ?? "");
            if (activeMenuMatch.IsMatch)
            {
                controller.ViewBag.ActiveCategory = activeMenuMatch.Category;
                controller.ViewBag.ActiveSubCategory = activeMenuMatch.SubCategory;
            }
        }

        int fixedIncomesNoticedCount = 0, fixedExpenditureNoticedCount = 0;
        int fixedIncomesExpiredCount = 0, fixedExpenditureExpiredCount = 0;

        if (sessionAccount != null && loggedInAccount.Role == Role.User)
        {
            try
            {
                var counts = await _dashboardService.GetNoticeCountsAsync(
                    loggedInAccount.Email ?? "",
                    _serverSettings.Value.NoticeMaturityDateDay,
                    loggedInAccount.TimeZoneIanaId ?? "UTC",
                    context.HttpContext.RequestAborted);

                fixedIncomesNoticedCount = counts.FixedIncomesNoticed;
                fixedExpenditureNoticedCount = counts.FixedExpendituresNoticed;
                fixedIncomesExpiredCount = counts.FixedIncomesExpired;
                fixedExpenditureExpiredCount = counts.FixedExpendituresExpired;
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to load notice counts for {Email}", loggedInAccount.Email); }
        }

        controller.ViewBag.FixedIncomesNoticedCount = fixedIncomesNoticedCount;
        controller.ViewBag.FixedExpenditureNoticedCount = fixedExpenditureNoticedCount;
        controller.ViewBag.FixedIncomesExpiredCount = fixedIncomesExpiredCount;
        controller.ViewBag.FixedExpenditureExpiredCount = fixedExpenditureExpiredCount;
        controller.ViewBag.FixedIncomesTotalNoticeCount = fixedIncomesNoticedCount + fixedIncomesExpiredCount;
        controller.ViewBag.FixedExpenditureTotalNoticeCount = fixedExpenditureNoticedCount + fixedExpenditureExpiredCount;
        controller.ViewBag.TotalNoticeCount = fixedIncomesNoticedCount + fixedExpenditureNoticedCount +
                                               fixedIncomesExpiredCount + fixedExpenditureExpiredCount;

        var requestCultureFeature = context.HttpContext.Features.Get<IRequestCultureFeature>();
        controller.ViewBag.CurrentCulture = requestCultureFeature?.RequestCulture.Culture.Name ?? "en-US";

        string requestPath = context.HttpContext.Request.Path.Value ?? "";
        controller.ViewBag.ReturnUri = string.IsNullOrWhiteSpace(requestPath) ? "/" : $"{requestPath}{context.HttpContext.Request.QueryString}";

        await next();
    }
}
