using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Abstract base controller that runs before every action to populate ViewBag with
/// the logged-in account, notification badge counts, current culture, and return URI.
/// </summary>
public abstract class BaseController : Controller
{
    private readonly IAccountService _accountService;
    private readonly IDashboardService _dashboardService;
    private readonly IOptions<ServerSetting> _serverSettings;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="BaseController"/> with the supplied dependencies.</summary>
    protected BaseController(
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

    /// <summary>Populates shared ViewBag entries (account, notice counts, culture, return URI) before each action executes.</summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>().CreateLogger<BaseController>();

        ViewBag.DomainName = _serverSettings.Value.DomainName;

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

        ViewBag.LoginedAccount = loginedAccount;

        // Fixed income/expenditure notice counts - delegated to DashboardService
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

        ViewBag.FixedIncomesNoticedCount = fixedIncomesNoticedCount;
        ViewBag.FixedExpenditureNoticedCount = fixedExpenditureNoticedCount;
        ViewBag.FixedIncomesExpiredCount = fixedIncomesExpiredCount;
        ViewBag.FixedExpenditureExpiredCount = fixedExpenditureExpiredCount;

        var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
        ViewBag.CurrentCulture = requestCultureFeature?.RequestCulture.Culture.Name ?? "en-US";

        string requestPath = HttpContext.Request.Path.Value ?? "";
        ViewBag.ReturnUri = string.IsNullOrWhiteSpace(requestPath) ? "/" : $"{requestPath}{HttpContext.Request.QueryString}";

        await next();
    }
}
