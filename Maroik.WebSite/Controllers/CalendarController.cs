using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Services;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Maroik.WebSite.Models.ViewModels.Calendar;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Hosting;
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
        private readonly ICalendarEventRepository _calendarEventRepository;
        private readonly IHostEnvironment _hostEnvironment;

        public CalendarController(IHtmlLocalizer<CalendarController> localizer, ILogger<CalendarController> logger, IHostEnvironment hostEnvironment, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IAssetRepository assetRepository, IAccountRepository accountRepository, ICalendarRepository calendarRepository, ICalendarEventRepository calendarEventRepository)
        {
            _localizer = localizer;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _incomeRepository = incomeRepository;
            _expenditureRepository = expenditureRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
            _calendarRepository = calendarRepository;
            _calendarEventRepository = calendarEventRepository;
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

        #region Calendar
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
                        tempCalendar.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                        tempCalendar.Created = DateTime.UtcNow;
                        tempCalendar.Updated = DateTime.UtcNow;

                        Calendar tempCalendarToValidate = (await _calendarRepository.GetCalendarsAsync(tempCalendar.AccountEmail)).Where(x => x.Name == (tempCalendar?.Name ?? "")).FirstOrDefault();
                        
                        if (tempCalendarToValidate is not null)
                        {
                            return Json(new { result = false, error = _localizer["The calendar already exists."].Value });
                        }

                        await _calendarRepository.CreateCalendarAsync(tempCalendar);

                        tempCalendar.AccountEmail = null;
                        tempCalendar.Description = null;
                        tempCalendar.TimeZoneIanaId = null;

                        return Json(new { result = true, message = _localizer["The calendar has been successfully created."].Value, calendar = tempCalendar });
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

        #region Calendar Event
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateCalendarEvent([FromForm] CalendarEventInputViewModel calendarEventInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte));

                        Calendar tempCalendarToValidate = (await _calendarRepository.GetCalendarsAsync(loginedAccount.Email)).Where(x => x.Id == calendarEventInputViewModel?.CalendarId).FirstOrDefault();

                        if (tempCalendarToValidate is null)
                        {
                            return Json(new { result = false, error = _localizer["The calendar does not exists."].Value });
                        }

                        #region CalendarEvent
                        CalendarEvent calendarEvent = new CalendarEvent();

                        if (calendarEventInputViewModel.AllDay)
                        {
                            DateTime tempStartDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[0]), 
                                Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[1]), 
                                Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);

                            DateTime tempEndDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[0]),
                                Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[1]),
                                Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);

                            calendarEvent.CalendarId = calendarEventInputViewModel.CalendarId;
                            calendarEvent.Title = calendarEventInputViewModel.Title;
                            calendarEvent.Description = calendarEventInputViewModel.Description;
                            calendarEvent.AllDay = calendarEventInputViewModel.AllDay;
                            calendarEvent.StartDate = tempStartDate;
                            calendarEvent.EndDate = tempEndDate;
                            calendarEvent.StartDateTimeZoneIanaId = null;
                            calendarEvent.EndDateTimeZoneIanaId = null;
                            calendarEvent.Location = calendarEventInputViewModel.Location;
                            calendarEvent.Status = calendarEventInputViewModel.Status;
                            //calendarEvent.RecurrenceId = 0;
                            calendarEvent.Created = DateTime.UtcNow;
                            calendarEvent.Updated = DateTime.UtcNow;

                        }
                        else
                        {
                            string[] temporaryStartDate = calendarEventInputViewModel.StartDate.Split(" ");
                            string[] temporaryEndDate = calendarEventInputViewModel.EndDate.Split(" ");

                            DateTime tempStartDate = new DateTime(Convert.ToInt32(temporaryStartDate[0].Split("-")[0]),
                                Convert.ToInt32(temporaryStartDate[0].Split("-")[1]),
                                Convert.ToInt32(temporaryStartDate[0].Split("-")[2]),
                                Convert.ToInt32(temporaryStartDate[1].Split(":")[0]),
                                Convert.ToInt32(temporaryStartDate[1].Split(":")[1]), 
                                0, DateTimeKind.Unspecified);

                            DateTime tempEndDate = new DateTime(Convert.ToInt32(temporaryEndDate[0].Split("-")[0]),
                                Convert.ToInt32(temporaryEndDate[0].Split("-")[1]),
                                Convert.ToInt32(temporaryEndDate[0].Split("-")[2]),
                                Convert.ToInt32(temporaryStartDate[1].Split(":")[0]),
                                Convert.ToInt32(temporaryStartDate[1].Split(":")[1]), 
                                0, DateTimeKind.Unspecified);

                            foreach (TimeZoneInfo systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                            {
                                string systemTimeZoneIanaId = string.Empty;

                                if (systemTimeZone.HasIanaId)
                                {
                                    systemTimeZoneIanaId = systemTimeZone.Id;
                                }
                                else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out string ianaId))
                                {
                                    systemTimeZoneIanaId = ianaId;
                                }

                                if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                {
                                    continue;
                                }
                                else if (systemTimeZoneIanaId == calendarEventInputViewModel.StartDateTimeZoneIanaId)
                                {
                                    tempStartDate = TimeZoneInfo.ConvertTime(tempStartDate, systemTimeZone, TimeZoneInfo.Utc);
                                    break;
                                }
                            }

                            foreach (TimeZoneInfo systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                            {
                                string systemTimeZoneIanaId = string.Empty;

                                if (systemTimeZone.HasIanaId)
                                {
                                    systemTimeZoneIanaId = systemTimeZone.Id;
                                }
                                else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out string ianaId))
                                {
                                    systemTimeZoneIanaId = ianaId;
                                }

                                if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                {
                                    continue;
                                }
                                else if (systemTimeZoneIanaId == calendarEventInputViewModel.EndDateTimeZoneIanaId)
                                {
                                    tempEndDate = TimeZoneInfo.ConvertTime(tempEndDate, systemTimeZone, TimeZoneInfo.Utc);
                                    break;
                                }
                            }

                            calendarEvent.CalendarId = calendarEventInputViewModel.CalendarId;
                            calendarEvent.Title = calendarEventInputViewModel.Title;
                            calendarEvent.Description = calendarEventInputViewModel.Description;
                            calendarEvent.AllDay = calendarEventInputViewModel.AllDay;
                            calendarEvent.StartDate = tempStartDate;
                            calendarEvent.EndDate = tempEndDate;
                            calendarEvent.StartDateTimeZoneIanaId = calendarEventInputViewModel.StartDateTimeZoneIanaId;
                            calendarEvent.EndDateTimeZoneIanaId = calendarEventInputViewModel.EndDateTimeZoneIanaId;
                            calendarEvent.Location = calendarEventInputViewModel.Location;
                            calendarEvent.Status = calendarEventInputViewModel.Status;
                            //calendarEvent.RecurrenceId = 0;
                            calendarEvent.Created = DateTime.UtcNow;
                            calendarEvent.Updated = DateTime.UtcNow;
                        }

                        await _calendarEventRepository.CreateCalendarEventAsync(calendarEvent);
                        #endregion

                        //List<Dictionary<string, object>> calendarReminders = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(calendarEventInputViewModel.SerializedCalendarReminders);

                        //foreach (var reminder in calendarReminders)
                        //{
                        //    string method = reminder["Method"].ToString();
                        //    int? minutesBeforeEvent = reminder["MinutesBeforeEvent"] != null ? (int?)Convert.ToInt32(reminder["MinutesBeforeEvent"]) : null;
                        //    int? hoursBeforeEvent = reminder["HoursBeforeEvent"] != null ? (int?)Convert.ToInt32(reminder["HoursBeforeEvent"]) : null;
                        //    int? daysBeforeEvent = reminder["DaysBeforeEvent"] != null ? (int?)Convert.ToInt32(reminder["DaysBeforeEvent"]) : null;
                        //    int? weeksBeforeEvent = reminder["WeeksBeforeEvent"] != null ? (int?)Convert.ToInt32(reminder["WeeksBeforeEvent"]) : null;
                        //    string timesBeforeEvent = reminder["TimesBeforeEvent"].ToString();

                        //    // Your logic to handle each reminder
                        //}

                        //Calendar tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new Calendar();
                        //tempCalendar.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                        //tempCalendar.Created = DateTime.UtcNow;
                        //tempCalendar.Updated = DateTime.UtcNow;

                        //Calendar tempCalendarToValidate = (await _calendarRepository.GetCalendarsAsync(tempCalendar.AccountEmail)).Where(x => x.Name == (tempCalendar?.Name ?? "")).FirstOrDefault();

                        //if (tempCalendarToValidate is not null)
                        //{
                        //    return Json(new { result = false, error = _localizer["The calendar already exists."].Value });
                        //}

                        //await _calendarRepository.CreateCalendarAsync(tempCalendar);

                        //tempCalendar.AccountEmail = null;
                        //tempCalendar.Description = null;
                        //tempCalendar.TimeZoneIanaId = null;

                        //return Json(new { result = true, message = _localizer["The calendar has been successfully created."].Value, calendar = tempCalendar });
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

        #region Summernote Image File Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
        {
            if (summernoteImageFile == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
            }
            else
            {
                if (summernoteImageFile.Length is > 0 and <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if (Path.GetExtension(summernoteImageFile.FileName) is ".png" or ".jpg" or ".jpeg")
                    {

                        string summernoteImagePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Calendar", "UserIndex", "summernote", "images");
                        if (!Directory.Exists(summernoteImagePath))
                        {
                            _ = Directory.CreateDirectory(summernoteImagePath);
                        }

                        string imageFile = $"{Guid.NewGuid().ToString().ToUpper()}{Path.GetExtension(summernoteImageFile.FileName)}";
                        string filePath = Path.Combine(summernoteImagePath, imageFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await summernoteImageFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }
                        return Ok(new { result = true, filePath = $"/upload/Calendar/UserIndex/summernote/images/{imageFile}" });
                    }
                    else // 이미지 png 또는 jpg 또는 jpeg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });
                    }
                }
                else
                {
                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                }
            }
        }
        #endregion

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
                    HtmlColorCode = "#fc330e", // Default Html Color Code
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                });
                await _calendarRepository.CreateCalendarAsync(calendars[0]);
            }
            #endregion

            calendarOutputViewModel.Calendars = calendars;

            return View(calendarOutputViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsCalendarExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                List<Calendar> tempCalendars = await _calendarRepository.GetCalendarsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email) ?? [];

                if (tempCalendars == null || tempCalendars.Count == 0)
                {
                    return Json(new { result = false, error = _localizer["No calendar exists"].Value });
                }
                else
                {
                    Calendar tempCalendar = tempCalendars.Where(a => a.Id == id).FirstOrDefault();

                    if (tempCalendar == null)
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        return Json(new { result = true, calendar = tempCalendar });
                    }
                }
            }
            catch
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> GetCalendars()
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                List<Calendar> calendars = (await _calendarRepository.GetCalendarsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email) ?? [])?.OrderBy(x => x.Id)?.ToList();

                return Json(new { result = true, calendars });
            }
            catch
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        Calendar tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new Calendar();
                        tempCalendar.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                        tempCalendar.Updated = DateTime.UtcNow;

                        List<Calendar> tempCalendars = await _calendarRepository.GetCalendarsAsync(tempCalendar.AccountEmail);
                        Calendar previousCalendar = tempCalendars.Where(a => a.Id == tempCalendar.Id).FirstOrDefault();
                        tempCalendar.Created = previousCalendar.Created;

                        if (previousCalendar.Id > 0) 
                        {
                            await _calendarRepository.UpdateCalendarAsync(tempCalendar);

                            return Json(new { result = true, message = _localizer["The calendar has been successfully updated."].Value, calendar = new Calendar() { Id = tempCalendar.Id, Name = tempCalendar.Name, HtmlColorCode = tempCalendar.HtmlColorCode } });
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

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                Calendar tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new Calendar();
                tempCalendar.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;

                List<Calendar> tempCalendars = await _calendarRepository.GetCalendarsAsync(tempCalendar.AccountEmail);
                if ((tempCalendars.Where(a => a.Id == tempCalendar.Id).FirstOrDefault()?.Id ?? 0) > 0)
                {
                    await _calendarRepository.DeleteCalendarAsync(tempCalendar);

                    return Json(new { result = true, message = _localizer["The calendar has been successfully deleted."].Value, calendar = new Calendar() { Id = tempCalendar.Id } });
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
