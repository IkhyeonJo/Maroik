using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

public abstract class BaseController : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = HttpContext.RequestServices;
        var accountRepo = services.GetRequiredService<IAccountRepository>();
        var dashboardService = services.GetRequiredService<IDashboardService>();
        var serverSettings = services.GetRequiredService<IOptions<ServerSetting>>();
        var sessionService = services.GetRequiredService<ISessionService>();

        ViewBag.DomainName = serverSettings.Value.DomainName;

        AccountResponse loginedAccount = new()
        {
            Nickname = "Login",
            Role = Role.Anonymous,
            TimeZoneIanaId = "UTC",
            AvatarImagePath = "/anonymous/images/bg1.jpg"
        };

        var sessionAccount = sessionService.GetAccount();
        if (sessionAccount != null)
        {
            try
            {
                string email = sessionAccount.Email ?? "";
                if (!string.IsNullOrEmpty(email))
                    loginedAccount = await accountRepo.GetAccountByEmailAsync(email) ?? loginedAccount;
            }
            catch { /* ignore session refresh errors */ }
        }

        ViewBag.LoginedAccount = loginedAccount;

        // Fixed income/expenditure notice counts — delegated to DashboardService
        int fixedIncomesNoticedCount = 0, fixedExpenditureNoticedCount = 0;
        int fixedIncomesExpiredCount = 0, fixedExpenditureExpiredCount = 0;

        if (sessionAccount != null && loginedAccount.Role == Role.User)
        {
            try
            {
                var counts = await dashboardService.GetNoticeCountsAsync(
                    loginedAccount.Email ?? "",
                    serverSettings.Value.NoticeMaturityDateDay);

                fixedIncomesNoticedCount = counts.FixedIncomesNoticed;
                fixedExpenditureNoticedCount = counts.FixedExpendituresNoticed;
                fixedIncomesExpiredCount = counts.FixedIncomesExpired;
                fixedExpenditureExpiredCount = counts.FixedExpendituresExpired;
            }
            catch { /* ignore — non-critical UI badge */ }
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
