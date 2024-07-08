using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Maroik.WebSite.Models.ViewModels.Calendar;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class CalendarController : Controller
    {
        private readonly IHtmlLocalizer<CalendarController> _localizer;
        private readonly ILogger<CalendarController> _logger;
        private readonly IIncomeRepository _incomeRepository;
        private readonly IExpenditureRepository _expenditureRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ICalendarRepository _calendarRepository;
        private readonly string _defaultHtmlcolorCode = "#fc330e";

        public CalendarController(IHtmlLocalizer<CalendarController> localizer, ILogger<CalendarController> logger, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IAssetRepository assetRepository, IAccountRepository accountRepository, ICalendarRepository calendarRepository)
        {
            _localizer = localizer;
            _logger = logger;
            _incomeRepository = incomeRepository;
            _expenditureRepository = expenditureRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _calendarRepository = calendarRepository;
        }

        #region Admin
        [HttpGet]
        public IActionResult AdminIndex()
        {
            return View();
        }
        #endregion

        #region User

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        Calendar tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new Calendar();

                        Calendar tempCalendarToValidate = (await _calendarRepository.GetCalendarsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)).Where(x => x.Name == (tempCalendar?.Name ?? "")).FirstOrDefault();
                        
                        if (tempCalendarToValidate is not null)
                        {
                            return Json(new { result = false, error = _localizer["The calendar already exists."].Value });
                        }

                        #region 달력 생성
                        await _calendarRepository.CreateCalendarAsync
                            (
                            new Calendar()
                            {
                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                Name = tempCalendar.Name,
                                Description = tempCalendar.Description,
                                TimeZoneIanaId = tempCalendar.TimeZoneIanaId,
                                HtmlcolorCode = _defaultHtmlcolorCode
                            }
                            );
                        #endregion

                        return Json(new { result = true, message = _localizer["The calendar has been successfully created."].Value });
                    }
                    catch
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> UserIndex()
        {
            _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte));

            CalendarOutputViewModel calendarOutputViewModel = new CalendarOutputViewModel();

            #region Load Calendar
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(loginedAccount.Email) ?? [];

            if (calendars.Count == 0)
            {
                calendars.Add(new Calendar
                {
                    AccountEmail = loginedAccount.Email,
                    Name = loginedAccount.Nickname,
                    TimeZoneIanaId = loginedAccount.TimeZoneIanaId,
                    HtmlcolorCode = _defaultHtmlcolorCode,
                });
                await _calendarRepository.CreateCalendarAsync(calendars[0]);
            }
            #endregion

            calendarOutputViewModel.Calendars = calendars;

            return View(calendarOutputViewModel);
        }
        #endregion

        #endregion

        #region Anonymous
        [HttpGet]
        public IActionResult AnonymousIndex()
        {
            return View();
        }
        #endregion
    }
}
