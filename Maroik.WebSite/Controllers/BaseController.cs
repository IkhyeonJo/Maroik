using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Client.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public abstract class BaseController : Controller
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = HttpContext.RequestServices;
        var accountRepo = services.GetRequiredService<IAccountRepository>();
        var fixedIncomeRepo = services.GetRequiredService<IFixedIncomeRepository>();
        var fixedExpenditureRepo = services.GetRequiredService<IFixedExpenditureRepository>();

        bool isSessionExist = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out byte[] resultByte);

        AccountResponse loginedAccount = new()
        {
            Nickname = "Login",
            Role = Role.Anonymous,
            TimeZoneIanaId = "UTC",
            AvatarImagePath = "/anonymous/images/bg1.jpg"
        };

        string loginedAccountTimeZoneIanaId = "UTC";

        if (isSessionExist && resultByte?.Length > 0)
        {
            try
            {
                string email = JsonConvert.DeserializeObject<AccountResponse>(
                    Encoding.Default.GetString(resultByte))?.Email ?? "";
                if (!string.IsNullOrEmpty(email))
                {
                    loginedAccount = await accountRepo.GetAccountByEmailAsync(email) ?? loginedAccount;
                    loginedAccountTimeZoneIanaId = loginedAccount.TimeZoneIanaId ?? "UTC";
                }
            }
            catch { /* ignore */ }
        }

        ViewBag.LoginedAccount = loginedAccount;

        // Fixed income/expenditure notice counts for _MainTopBar
        int fixedIncomesNoticedCount = 0, fixedExpenditureNoticedCount = 0;
        int fixedIncomesExpiredCount = 0, fixedExpenditureExpiredCount = 0;

        if (isSessionExist && resultByte?.Length > 0 && loginedAccount.Role == Role.User)
        {
            try
            {
                DateTime currentDate = DateTime.UtcNow.Date;

                foreach (var item in await fixedIncomeRepo.GetFixedIncomesAsync(loginedAccount.Email ?? ""))
                {
                    bool noticed = false;
                    try
                    {
                        noticed = currentDate.Subtract(new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay)).TotalDays <= 0 &&
                                  Math.Abs(currentDate.Subtract(new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                    }
                    catch { noticed = false; }
                    if (item.Unpunctuality) noticed = true;
                    if (noticed) fixedIncomesNoticedCount++;
                    if (item.MaturityDate.Subtract(currentDate).TotalDays < 0) fixedIncomesExpiredCount++;
                }

                foreach (var item in await fixedExpenditureRepo.GetFixedExpendituresAsync(loginedAccount.Email ?? ""))
                {
                    bool noticed = false;
                    try
                    {
                        noticed = currentDate.Subtract(new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay)).TotalDays <= 0 &&
                                  Math.Abs(currentDate.Subtract(new DateTime(currentDate.Year, item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                    }
                    catch { noticed = false; }
                    if (item.Unpunctuality) noticed = true;
                    if (noticed) fixedExpenditureNoticedCount++;
                    if (item.MaturityDate.Subtract(currentDate).TotalDays < 0) fixedExpenditureExpiredCount++;
                }
            }
            catch { /* ignore */ }
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
