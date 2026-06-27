using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Maroik.WebSite.Contracts;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Filters;

/// <summary>
/// Global action filter that populates shared ViewBag entries before every action executes:
/// domain name, file-size limit, logged-in account, notification badge counts, current culture,
/// and the return URI.
/// </summary>
public class ViewBagPopulatorFilter : IAsyncActionFilter
{
    private readonly IAccountService _accountService;
    private readonly IDashboardService _dashboardService;
    private readonly IOptions<ServerSetting> _serverSettings;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="ViewBagPopulatorFilter"/> with the supplied dependencies.</summary>
    public ViewBagPopulatorFilter(
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
    {
        _accountService = accountService;
        _dashboardService = dashboardService;
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

        AccountResponse loginedAccount = new()
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
                    loginedAccount = await _accountService.GetAccountByEmailAsync(email, context.HttpContext.RequestAborted) ?? loginedAccount;
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to refresh session account for {Email}", sessionAccount.Email); }
        }

        controller.ViewBag.LoginedAccount = loginedAccount;

        int fixedIncomesNoticedCount = 0, fixedExpenditureNoticedCount = 0;
        int fixedIncomesExpiredCount = 0, fixedExpenditureExpiredCount = 0;

        if (sessionAccount != null && loginedAccount.Role == Role.User)
        {
            try
            {
                var counts = await _dashboardService.GetNoticeCountsAsync(
                    loginedAccount.Email ?? "",
                    _serverSettings.Value.NoticeMaturityDateDay,
                    context.HttpContext.RequestAborted);

                fixedIncomesNoticedCount = counts.FixedIncomesNoticed;
                fixedExpenditureNoticedCount = counts.FixedExpendituresNoticed;
                fixedIncomesExpiredCount = counts.FixedIncomesExpired;
                fixedExpenditureExpiredCount = counts.FixedExpendituresExpired;
            }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to load notice counts for {Email}", loginedAccount.Email); }
        }

        controller.ViewBag.FixedIncomesNoticedCount = fixedIncomesNoticedCount;
        controller.ViewBag.FixedExpenditureNoticedCount = fixedExpenditureNoticedCount;
        controller.ViewBag.FixedIncomesExpiredCount = fixedIncomesExpiredCount;
        controller.ViewBag.FixedExpenditureExpiredCount = fixedExpenditureExpiredCount;

        var requestCultureFeature = context.HttpContext.Features.Get<IRequestCultureFeature>();
        controller.ViewBag.CurrentCulture = requestCultureFeature?.RequestCulture.Culture.Name ?? "en-US";

        string requestPath = context.HttpContext.Request.Path.Value ?? "";
        controller.ViewBag.ReturnUri = string.IsNullOrWhiteSpace(requestPath) ? "/" : $"{requestPath}{context.HttpContext.Request.QueryString}";

        await next();
    }
}
