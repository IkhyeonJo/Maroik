using Mapster;
using Ganss.Xss;
using HtmlAgilityPack;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Calendar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages calendars and calendar events: CRUD, sharing, subscriptions,
/// Summernote image uploads, and role-differentiated index views.
/// </summary>
public class CalendarController(
    IHtmlLocalizer<CalendarController> localizer,
    ICalendarService calendarService,
    IRsaService rsa,
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    private readonly ISessionService _sessionService = sessionService;

    #region Calendar

    #region Create

    #region Calendar
    /// <summary>Creates a new calendar.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            var tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email ?? string.Empty;

            var result = await calendarService.CreateCalendarAsync(tempCalendar);

            if (!result.Success)
                return Json(new { result = false, error = localizer[result.ErrorKey].Value });

            tempCalendar.AccountEmail = null;
            tempCalendar.Description = null;
            tempCalendar.TimeZoneIanaId = null;
            return Json(new { result = true, message = localizer["The calendar has been successfully created."].Value, calendar = tempCalendar });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Calendar Event
    /// <summary>Creates a new calendar event (with optional file attachment and reminders).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateCalendarEvent([FromForm] CalendarEventInputViewModel calendarEventInputViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                calendarEventInputViewModel.Description ??= ""; // If no content was entered

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                var sanitized = sanitizer.Sanitize(calendarEventInputViewModel.Description);
                calendarEventInputViewModel.Description = sanitized;
                #endregion

                #region Decrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(calendarEventInputViewModel.Description);

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                {
                    var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", rsa.Decrypt(encryptedFilePath));
                }

                calendarEventInputViewModel.Description = htmlDocument.DocumentNode.OuterHtml;
                #endregion

                if (calendarEventInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = localizer["Name length is must be between 1 and 100 characters."].Value });
                }

                if (calendarEventInputViewModel.Description.Length > 16384) // Description Maxlength: 16KB
                {
                    return Json(new { result = false, error = localizer["Description length is must be between 0 and 16384 characters."].Value });
                }

                if (calendarEventInputViewModel.CalendarEventUploadedFile != null) // If a file is attached
                {
                    if (Path.GetExtension(calendarEventInputViewModel.CalendarEventUploadedFile.FileName) != ".zip") // Only zip extension allowed
                    {
                        return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                    }

                    if (calendarEventInputViewModel.CalendarEventUploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }

                var isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = localizer["Please Login to create calendar event"].Value });

                var loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = localizer["Please Login to create calendar event"].Value });

                #region Build CalendarEventRequest with UTC dates
                CalendarEventRequest calendarEventRequest = new();

                if (calendarEventInputViewModel.AllDay)
                {
                    calendarEventRequest.StartDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[0]),
                        Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[1]),
                        Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);
                    calendarEventRequest.EndDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[0]),
                        Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[1]),
                        Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);
                    calendarEventRequest.StartDateTimeZoneIanaId = null;
                    calendarEventRequest.EndDateTimeZoneIanaId = null;
                }
                else
                {
                    var temporaryStartDate = calendarEventInputViewModel.StartDate.Split(" ");
                    var temporaryEndDate = calendarEventInputViewModel.EndDate.Split(" ");
                    DateTime tempStartDate = new(Convert.ToInt32(temporaryStartDate[0].Split("-")[0]),
                        Convert.ToInt32(temporaryStartDate[0].Split("-")[1]),
                        Convert.ToInt32(temporaryStartDate[0].Split("-")[2]),
                        Convert.ToInt32(temporaryStartDate[1].Split(":")[0]),
                        Convert.ToInt32(temporaryStartDate[1].Split(":")[1]),
                        0, DateTimeKind.Unspecified);
                    DateTime tempEndDate = new(Convert.ToInt32(temporaryEndDate[0].Split("-")[0]),
                        Convert.ToInt32(temporaryEndDate[0].Split("-")[1]),
                        Convert.ToInt32(temporaryEndDate[0].Split("-")[2]),
                        Convert.ToInt32(temporaryEndDate[1].Split(":")[0]),
                        Convert.ToInt32(temporaryEndDate[1].Split(":")[1]),
                        0, DateTimeKind.Unspecified);

                    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var tzId = tz.HasIanaId ? tz.Id : TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out var id) ? id : string.Empty;
                        if (string.IsNullOrEmpty(tzId) || tzId != calendarEventInputViewModel.StartDateTimeZoneIanaId)
                            continue;
                        tempStartDate = TimeZoneInfo.ConvertTime(tempStartDate, tz, TimeZoneInfo.Utc); break;
                    }
                    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var tzId = tz.HasIanaId ? tz.Id : TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out var id) ? id : string.Empty;
                        if (string.IsNullOrEmpty(tzId) || tzId != calendarEventInputViewModel.EndDateTimeZoneIanaId)
                            continue;
                        tempEndDate = TimeZoneInfo.ConvertTime(tempEndDate, tz, TimeZoneInfo.Utc); break;
                    }

                    calendarEventRequest.StartDate = tempStartDate;
                    calendarEventRequest.EndDate = tempEndDate;
                    calendarEventRequest.StartDateTimeZoneIanaId = calendarEventInputViewModel.StartDateTimeZoneIanaId;
                    calendarEventRequest.EndDateTimeZoneIanaId = calendarEventInputViewModel.EndDateTimeZoneIanaId;
                }

                calendarEventRequest.CalendarId = calendarEventInputViewModel.CalendarId;
                calendarEventRequest.Title = calendarEventInputViewModel.Title;
                calendarEventRequest.Description = calendarEventInputViewModel.Description;
                calendarEventRequest.AllDay = calendarEventInputViewModel.AllDay;
                calendarEventRequest.Location = calendarEventInputViewModel.Location;
                calendarEventRequest.Status = calendarEventInputViewModel.Status;
                #endregion

                #region Build reminders
                List<CalendarReminderInfo> reminders = [];
                var calendarReminders = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(calendarEventInputViewModel.SerializedCalendarReminders);
                reminders.AddRange(from reminder in calendarReminders
                    let tempTimes = reminder["TimesBeforeEvent"]?.ToString()
                    select new CalendarReminderInfo
                    {
                        Method = reminder["Method"].ToString() ?? string.Empty,
                        MinutesBeforeEvent = reminder["MinutesBeforeEvent"] != null ? Convert.ToInt32(reminder["MinutesBeforeEvent"]) : null,
                        HoursBeforeEvent = reminder["HoursBeforeEvent"] != null ? Convert.ToInt32(reminder["HoursBeforeEvent"]) : null,
                        DaysBeforeEvent = reminder["DaysBeforeEvent"] != null ? Convert.ToInt32(reminder["DaysBeforeEvent"]) : null,
                        WeeksBeforeEvent = reminder["WeeksBeforeEvent"] != null ? Convert.ToInt32(reminder["WeeksBeforeEvent"]) : null,
                        TimesBeforeEvent = !string.IsNullOrEmpty(tempTimes)
                            ? new TimeSpan(Convert.ToInt32(tempTimes.Split(":")[0]), Convert.ToInt32(tempTimes.Split(":")[1]), 0)
                            : null
                    });

                #endregion

                #region Build attached file
                AttachedFileInfo attachedFile = null;
                if (calendarEventInputViewModel.CalendarEventUploadedFile != null)
                {
                    using MemoryStream ms = new();
                    await calendarEventInputViewModel.CalendarEventUploadedFile.CopyToAsync(ms);
                    attachedFile = new AttachedFileInfo
                    {
                        Bytes = ms.ToArray(),
                        ContentType = calendarEventInputViewModel.CalendarEventUploadedFile.ContentType,
                        FileName = calendarEventInputViewModel.CalendarEventUploadedFile.FileName,
                        Size = calendarEventInputViewModel.CalendarEventUploadedFile.Length
                    };
                }
                #endregion

                var createEventResult = await calendarService.CreateCalendarEventAsync(
                    calendarEventRequest,
                    loginedAccount.Email ?? string.Empty,
                    $"{loginedAccount.Role}Index",
                    reminders,
                    attachedFile);

                return !createEventResult.Success ? Json(new { result = false, error = localizer[createEventResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar event has been successfully created."].Value });
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Summernote Image File Upload
    /// <summary>Uploads a Summernote inline image for use inside a calendar event description.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
    {
        if (summernoteImageFile == null)
            return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return Ok(new { result = false, errorMessage = localizer["Only .jpg or jpeg or .png file allowed."].Value });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await summernoteImageFile.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        var loginedAccount = _sessionService.GetAccount()!;
        var roleIndex = $"{loginedAccount.Role}Index";

        var file = new AttachedFileInfo
        {
            Bytes = bytes,
            ContentType = summernoteImageFile.ContentType,
            FileName = summernoteImageFile.FileName,
            Size = summernoteImageFile.Length
        };

        var uploadResult = await calendarService.UploadSummernoteImageAsync(file, roleIndex);

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        return Ok(new
        {
            result = true,
            file = File(uploadResult.FileBytes!, uploadResult.ContentType!, uploadResult.FileName!),
            filePath = rsa.Encrypt(uploadResult.FilePath!)
        });
    }
    #endregion

    #endregion

    #region Read
    
    /// <summary>Returns the admin calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> AdminIndex()
    {
        var loginedAccount = _sessionService.GetAccount()!;

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load Calendar
        var calendars = await calendarService.GetCalendarsAsync(loginedAccount.Email ?? string.Empty);

        if (calendars.Count == 0)
        {
            await calendarService.EnsureDefaultCalendarAsync(new CalendarRequest
            {
                AccountEmail = loginedAccount.Email,
                Name = loginedAccount.Nickname,
                TimeZoneIanaId = loginedAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e" // Default HTML Color Code
            });
            calendars = await calendarService.GetCalendarsAsync(loginedAccount.Email ?? string.Empty);
        }
        #endregion

        #region Load CalendarEvent
        List<CalendarEventOutputViewModel> calendarEventOutputViewModels = [];

        foreach (var calendar in calendars)
        {
            foreach (var calendarEvent in await calendarService.GetCalendarEventsAsync(calendar.Id))
            {
                CalendarEventOutputViewModel item = new()
                {
                    Id = calendarEvent.Id,
                    CalendarId = calendarEvent.CalendarId,
                    Title = calendarEvent.Title,
                    AllDay = calendarEvent.AllDay
                };

                if (calendarEvent.AllDay)
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd");
                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd");

                    item.StartDate = calendarEvent.StartDate;
                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == calendarEvent.EndDateTimeZoneIanaId)
                        {
                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");

                    item.StartDate = calendarEvent.StartDate;
                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }

                item.HtmlColorCode = calendar.HtmlColorCode;

                calendarEventOutputViewModels.Add(item);
            }
        }
        #endregion

        calendarOutputViewModel.Calendars = calendars;
        calendarOutputViewModel.CalendarEventOutputViewModels = calendarEventOutputViewModels;

        // Populate time data for view
        calendarOutputViewModel.LoginedAccount = loginedAccount;
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount.TimeZoneIanaId ?? "UTC";
        {
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = ct.Hour;
            calendarOutputViewModel.CurrentMinute = ct.Minute;
            calendarOutputViewModel.CurrentSecond = ct.Second;
            List<string> ti = [];
            for (var h = 0; h < 24; h++)
                for (var m = 0; m < 60; m += 15)
                    ti.Add($"{h:D2}:{m:D2}");
            calendarOutputViewModel.TimeIntervals = ti;
            var nm = (int)Math.Round(ct.Minute / 15.0) * 15;
            if (nm == 60) { ct = ct.AddHours(1); nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{ct.Hour:D2}:{nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    /// <summary>Returns the user calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> UserIndex()
    {
        var loginedAccount = _sessionService.GetAccount()!;

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load Calendar
        var calendars = await calendarService.GetCalendarsAsync(loginedAccount.Email ?? string.Empty);

        if (calendars.Count == 0)
        {
            await calendarService.EnsureDefaultCalendarAsync(new CalendarRequest
            {
                AccountEmail = loginedAccount.Email,
                Name = loginedAccount.Nickname,
                TimeZoneIanaId = loginedAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e" // Default HTML Color Code
            });
            calendars = await calendarService.GetCalendarsAsync(loginedAccount.Email ?? string.Empty);
        }
        #endregion

        #region Load CalendarEvent
        List<CalendarEventOutputViewModel> calendarEventOutputViewModels = [];

        foreach (var calendar in calendars)
        {
            foreach (var calendarEvent in await calendarService.GetCalendarEventsAsync(calendar.Id))
            {
                CalendarEventOutputViewModel item = new()
                {
                    Id = calendarEvent.Id,
                    CalendarId = calendarEvent.CalendarId,
                    Title = calendarEvent.Title,
                    AllDay = calendarEvent.AllDay
                };

                if (calendarEvent.AllDay)
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd");
                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd");

                    item.StartDate = calendarEvent.StartDate;
                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == calendarEvent.EndDateTimeZoneIanaId)
                        {
                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");

                    item.StartDate = calendarEvent.StartDate;
                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }

                item.HtmlColorCode = calendar.HtmlColorCode;

                calendarEventOutputViewModels.Add(item);
            }
        }
        #endregion

        #region Load OtherCalendar
        List<CalendarResponse> otherCalendars = [];

        var allCalendars = await calendarService.GetAllCalendarsAsync();

        var tempOtherCalendars = allCalendars.Join(
            await calendarService.GetOtherCalendarsAsync(loginedAccount.Email ?? string.Empty), calendar => calendar.Id,
            otherCalendar => otherCalendar.CalendarId,
            (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

        otherCalendars.AddRange(tempOtherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));
        #endregion

        #region Load OtherCalendarEvent
        List<CalendarEventOutputViewModel> otherCalendarEventOutputViewModels = [];

        foreach (var otherCalendar in otherCalendars)
        {
            foreach (var otherCalendarEvent in await calendarService.GetCalendarEventsAsync(otherCalendar.Id))
            {
                CalendarEventOutputViewModel item = new()
                {
                    Id = otherCalendarEvent.Id,
                    CalendarId = otherCalendarEvent.CalendarId,
                    Title = otherCalendarEvent.Title,
                    AllDay = otherCalendarEvent.AllDay
                };

                if (otherCalendarEvent.AllDay)
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    item.DisplayStartDate = otherCalendarEvent.StartDate.ToString("yyyy-MM-dd");
                    item.DisplayEndDate = otherCalendarEvent.EndDate.ToString("yyyy-MM-dd");

                    item.StartDate = otherCalendarEvent.StartDate;
                    var tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == otherCalendarEvent.StartDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == otherCalendarEvent.EndDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    item.DisplayStartDate = otherCalendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                    item.DisplayEndDate = otherCalendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");

                    item.StartDate = otherCalendarEvent.StartDate;
                    var tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }

                item.HtmlColorCode = otherCalendar.HtmlColorCode;

                otherCalendarEventOutputViewModels.Add(item);
            }
        }
        #endregion

        calendarOutputViewModel.Calendars = calendars;
        calendarOutputViewModel.CalendarEventOutputViewModels = calendarEventOutputViewModels;
        calendarOutputViewModel.OtherCalendars = otherCalendars;
        calendarOutputViewModel.OtherCalendarEventOutputViewModels = otherCalendarEventOutputViewModels;

        // Populate time data for view
        calendarOutputViewModel.LoginedAccount = loginedAccount;
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount.TimeZoneIanaId ?? "UTC";
        {
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = ct.Hour;
            calendarOutputViewModel.CurrentMinute = ct.Minute;
            calendarOutputViewModel.CurrentSecond = ct.Second;
            List<string> ti = [];
            for (var h = 0; h < 24; h++)
                for (var m = 0; m < 60; m += 15)
                    ti.Add($"{h:D2}:{m:D2}");
            calendarOutputViewModel.TimeIntervals = ti;
            var nm = (int)Math.Round(ct.Minute / 15.0) * 15;
            if (nm == 60) { ct = ct.AddHours(1); nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{ct.Hour:D2}:{nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    /// <summary>Returns the anonymous calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> AnonymousIndex()
    {
        AccountResponse loginedAccount = new() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load OtherCalendar
        List<CalendarResponse> otherCalendars = [];

        var allCalendars = await calendarService.GetAllCalendarsAsync();

        var tempOtherCalendars = allCalendars.Join(
            (await calendarService.GetCalendarSharedAsync()).Where(x => x.Anonymous).ToList(), calendar => calendar.Id,
            calendarShared => calendarShared.CalendarId,
            (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

        otherCalendars.AddRange(tempOtherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));

        #endregion

        #region Load OtherCalendarEvent
        List<CalendarEventOutputViewModel> otherCalendarEventOutputViewModels = [];

        foreach (var otherCalendar in otherCalendars)
        {
            foreach (var otherCalendarEvent in await calendarService.GetCalendarEventsAsync(otherCalendar.Id))
            {
                CalendarEventOutputViewModel item = new()
                {
                    Id = otherCalendarEvent.Id,
                    CalendarId = otherCalendarEvent.CalendarId,
                    Title = otherCalendarEvent.Title,
                    AllDay = otherCalendarEvent.AllDay
                };

                if (otherCalendarEvent.AllDay)
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = string.Empty;
                            break;
                        }
                    }

                    item.DisplayStartDate = otherCalendarEvent.StartDate.ToString("yyyy-MM-dd");
                    item.DisplayEndDate = otherCalendarEvent.EndDate.ToString("yyyy-MM-dd");

                    item.StartDate = otherCalendarEvent.StartDate;
                    var tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == otherCalendarEvent.StartDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var systemTimeZoneIanaId = string.Empty;

                        if (systemTimeZone.HasIanaId)
                        {
                            systemTimeZoneIanaId = systemTimeZone.Id;
                        }
                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                        {
                            systemTimeZoneIanaId = ianaId;
                        }

                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                        {
                        }
                        else if (systemTimeZoneIanaId == otherCalendarEvent.EndDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                            break;
                        }
                    }

                    item.DisplayStartDate = otherCalendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                    item.DisplayEndDate = otherCalendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");

                    item.StartDate = otherCalendarEvent.StartDate;
                    var tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }

                item.HtmlColorCode = otherCalendar.HtmlColorCode;

                otherCalendarEventOutputViewModels.Add(item);
            }
        }
        #endregion

        calendarOutputViewModel.OtherCalendars = otherCalendars;
        calendarOutputViewModel.OtherCalendarEventOutputViewModels = otherCalendarEventOutputViewModels;

        // Populate time data for view
        calendarOutputViewModel.LoginedAccount = loginedAccount;
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount.TimeZoneIanaId ?? "UTC";
        {
            var ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = ct.Hour;
            calendarOutputViewModel.CurrentMinute = ct.Minute;
            calendarOutputViewModel.CurrentSecond = ct.Second;
            List<string> ti = [];
            for (var h = 0; h < 24; h++)
                for (var m = 0; m < 60; m += 15)
                    ti.Add($"{h:D2}:{m:D2}");
            calendarOutputViewModel.TimeIntervals = ti;
            var nm = (int)Math.Round(ct.Minute / 15.0) * 15;
            if (nm == 60) { ct = ct.AddHours(1); nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{ct.Hour:D2}:{nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    /// <summary>Checks whether a calendar with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsCalendarExists(int id)
    {
        try
        {
            var tempCalendars = await calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            if (tempCalendars.Count == 0)
            {
                return Json(new { result = false, error = localizer["No calendar exists"].Value });
            }

            var tempCalendar = tempCalendars.FirstOrDefault(a => a.Id == id);

            return tempCalendar == null
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : (IActionResult)Json(new { result = true, calendar = tempCalendar });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns the list of the current user's calendars as JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetCalendars()
    {
        try
        {
            var calendars = await calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

            return Json(new { result = true, calendars });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns the list of shared/other calendars as JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> GetOtherCalendars()
    {
        try
        {
            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (isAccountSessionExist)
            {
                var calendars = await calendarService.GetAllCalendarsAsync();
                var otherCalendars = await calendarService.GetOtherCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

                var tempOtherCalendars = calendars.Join(otherCalendars, calendar => calendar.Id,
                    otherCalendar => otherCalendar.CalendarId,
                    (calendar, _) => new CalendarResponse()
                    {
                        Id = calendar.Id, Name = calendar.Name, HtmlColorCode = calendar.HtmlColorCode,
                    }).OrderBy(x => x.Name).ToList();

                return Json(new { result = true, tempOtherCalendars });
            }
            else
            {
                List<CalendarResponse> tempOtherCalendars = [];
                var temporaryOtherCalendars = (await calendarService.GetAllCalendarsAsync()).Join(
                    (await calendarService.GetCalendarSharedAsync()).Where(x => x.Anonymous).ToList(),
                    calendar => calendar.Id, calendarShared => calendarShared.CalendarId,
                    (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

                tempOtherCalendars.AddRange(temporaryOtherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));

                return Json(new { result = true, tempOtherCalendars });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Checks whether a calendar event with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsCalendarEventExists(int id)
    {
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var loginedAccount = _sessionService.GetAccount()!;

                    var calendars = await calendarService.GetCalendarsAsync(loginedAccount.Email ?? string.Empty);

                    var tempCalendarEvent = await calendarService.GetCalendarEventAsync(id);
                    var tempCalendar = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent?.CalendarId) ?? new CalendarResponse();

                    if (!(tempCalendarEvent?.Id > 0) || !(tempCalendar.Id > 0))
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                    CalendarEventOutputViewModel calendarEvent = new()
                    {
                        Id = tempCalendarEvent.Id,
                        CalendarId = tempCalendarEvent.CalendarId,
                        Title = tempCalendarEvent.Title,
                        AllDay = tempCalendarEvent.AllDay
                    };

                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(tempCalendarEvent.Description ?? string.Empty);

                    foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                    {
                        var filePath = imgTag.GetAttributeValue("alt", "");

                        // Method to download file
                        var fileData = await calendarService.DownloadFileAsync(filePath);

                        if (fileData == null) continue;
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Default contentType is set to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        var base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", rsa.Encrypt(filePath));
                    }

                    calendarEvent.Description = htmlDocument.DocumentNode.OuterHtml;

                    if (tempCalendarEvent.AllDay)
                    {
                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = string.Empty;
                                break;
                            }
                        }

                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.EndDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayEndDateTimeZone = string.Empty;
                                break;
                            }
                        }

                        calendarEvent.DisplayStartDate = tempCalendarEvent.StartDate.ToString("yyyy-MM-dd");
                        calendarEvent.DisplayEndDate = tempCalendarEvent.EndDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == tempCalendarEvent.StartDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                                break;
                            }
                        }

                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == tempCalendarEvent.EndDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.EndDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                                break;
                            }
                        }

                        calendarEvent.DisplayStartDate = tempCalendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                        calendarEvent.DisplayEndDate = tempCalendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");
                    }

                    calendarEvent.Location = tempCalendarEvent.Location;
                    calendarEvent.Status = tempCalendarEvent.Status;
                    calendarEvent.CalendarEventAttachedFile = await calendarService.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id);

                    if (!string.IsNullOrEmpty(calendarEvent.CalendarEventAttachedFile?.Path ?? ""))
                    {
                        var fileData = await calendarService.DownloadFileAsync(calendarEvent.CalendarEventAttachedFile?.Path ?? "");

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(calendarEvent.CalendarEventAttachedFile?.Path ?? "", out var contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                            calendarEvent.CalendarEventAttachedFileContentType = contentType;
                        }
                    }

                    calendarEvent.Calendars = calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name).ToList();
                    calendarEvent.SerializedCalendarReminders = JsonConvert.SerializeObject((await calendarService.GetCalendarEventRemindersAsync(tempCalendarEvent.Id)).Select(x => new CalendarReminderInfo { Method = x.Method ?? "", MinutesBeforeEvent = (int?)x.MinutesBeforeEvent, HoursBeforeEvent = (int?)x.HoursBeforeEvent, DaysBeforeEvent = (int?)x.DaysBeforeEvent, WeeksBeforeEvent = (int?)x.WeeksBeforeEvent, TimesBeforeEvent = x.TimesBeforeEvent?.ToTimeSpan() }));

                    calendarEvent.HtmlColorCode = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId)?.HtmlColorCode;

                    return Json(new { result = true, calendarEvent });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Checks whether an event in a shared/other calendar exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> IsOtherCalendarEventExists(int id)
    {
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var isAccountSessionExist = _sessionService.GetAccount() != null;
                    AccountResponse loginedAccount;
                    List<CalendarResponse> calendars;

                    if (isAccountSessionExist)
                    {
                        loginedAccount = _sessionService.GetAccount()!;
                        calendars = (await calendarService.GetAllCalendarsAsync()).Join(
                            await calendarService.GetOtherCalendarsAsync(loginedAccount.Email ?? string.Empty),
                            calendar => calendar.Id, otherCalendar => otherCalendar.CalendarId,
                            (calendar, _) => calendar).ToList();
                    }
                    else
                    {
                        loginedAccount = new AccountResponse() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
                        calendars = [];
                        var temporaryOtherCalendars = (await calendarService.GetAllCalendarsAsync()).Join(
                            (await calendarService.GetCalendarSharedAsync()).Where(x => x.Anonymous).ToList(),
                            calendar => calendar.Id, calendarShared => calendarShared.CalendarId,
                            (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

                        calendars.AddRange(temporaryOtherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));
                    }

                    var tempCalendarEvent = await calendarService.GetCalendarEventAsync(id);
                    var tempCalendar = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent?.CalendarId) ?? new CalendarResponse();

                    if (!(tempCalendarEvent?.Id > 0) || !(tempCalendar.Id > 0))
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                    CalendarEventOutputViewModel calendarEvent = new()
                    {
                        Id = tempCalendarEvent.Id,
                        CalendarId = tempCalendarEvent.CalendarId,
                        Title = tempCalendarEvent.Title,
                        AllDay = tempCalendarEvent.AllDay
                    };

                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(tempCalendarEvent.Description ?? string.Empty);

                    foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                    {
                        var filePath = imgTag.GetAttributeValue("alt", "");

                        // Method to download file
                        var fileData = await calendarService.DownloadFileAsync(filePath);

                        if (fileData == null) continue;
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Default contentType is set to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        var base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", rsa.Encrypt(filePath));
                    }

                    calendarEvent.Description = htmlDocument.DocumentNode.OuterHtml;

                    if (tempCalendarEvent.AllDay)
                    {
                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = string.Empty;
                                break;
                            }
                        }

                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.EndDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayEndDateTimeZone = string.Empty;
                                break;
                            }
                        }

                        calendarEvent.DisplayStartDate = tempCalendarEvent.StartDate.ToString("yyyy-MM-dd");
                        calendarEvent.DisplayEndDate = tempCalendarEvent.EndDate.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == tempCalendarEvent.StartDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                                break;
                            }
                        }

                        foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                        {
                            var systemTimeZoneIanaId = string.Empty;

                            if (systemTimeZone.HasIanaId)
                            {
                                systemTimeZoneIanaId = systemTimeZone.Id;
                            }
                            else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                            {
                                systemTimeZoneIanaId = ianaId;
                            }

                            if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                            {
                            }
                            else if (systemTimeZoneIanaId == tempCalendarEvent.EndDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.EndDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.EndDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                                break;
                            }
                        }

                        calendarEvent.DisplayStartDate = tempCalendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                        calendarEvent.DisplayEndDate = tempCalendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");
                    }

                    calendarEvent.Location = tempCalendarEvent.Location;
                    calendarEvent.Status = tempCalendarEvent.Status;
                    calendarEvent.CalendarEventAttachedFile = await calendarService.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id);

                    if (!string.IsNullOrEmpty(calendarEvent.CalendarEventAttachedFile?.Path ?? ""))
                    {
                        var fileData = await calendarService.DownloadFileAsync(calendarEvent.CalendarEventAttachedFile?.Path ?? "");

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(calendarEvent.CalendarEventAttachedFile?.Path ?? "", out var contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                            calendarEvent.CalendarEventAttachedFileContentType = contentType;
                        }
                    }

                    calendarEvent.Calendars = calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name).ToList();
                    calendarEvent.SerializedCalendarReminders = JsonConvert.SerializeObject((await calendarService.GetCalendarEventRemindersAsync(tempCalendarEvent.Id)).Select(x => new CalendarReminderInfo { Method = x.Method ?? "", MinutesBeforeEvent = (int?)x.MinutesBeforeEvent, HoursBeforeEvent = (int?)x.HoursBeforeEvent, DaysBeforeEvent = (int?)x.DaysBeforeEvent, WeeksBeforeEvent = (int?)x.WeeksBeforeEvent, TimesBeforeEvent = x.TimesBeforeEvent?.ToTimeSpan() }));

                    calendarEvent.HtmlColorCode = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId)?.HtmlColorCode;

                    return Json(new { result = true, calendarEvent });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns calendar events for the given calendar as JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> GetCalendarEvents([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    List<CalendarEventOutputViewModel> calendarEvents = [];

                    var isAccountSessionExist = _sessionService.GetAccount() != null;
                    AccountResponse loginedAccount;

                    List<CalendarResponse> tempCalendars;

                    List<CalendarResponse> tempOtherCalendars;

                    if (isAccountSessionExist)
                    {
                        loginedAccount = _sessionService.GetAccount()!;

                        tempCalendars = await calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

                        tempOtherCalendars = [];

                        var otherCalendars = (await calendarService.GetAllCalendarsAsync()).Join(
                            await calendarService.GetOtherCalendarsAsync(loginedAccount.Email ?? string.Empty),
                            calendar => calendar.Id, otherCalendar => otherCalendar.CalendarId,
                            (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

                        tempOtherCalendars.AddRange(otherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));
                    }
                    else
                    {
                        loginedAccount = new AccountResponse() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };

                        tempCalendars = [];

                        tempOtherCalendars = [];

                        var temporaryOtherCalendars = (await calendarService.GetAllCalendarsAsync()).Join(
                            (await calendarService.GetCalendarSharedAsync()).Where(x => x.Anonymous).ToList(),
                            calendar => calendar.Id, calendarShared => calendarShared.CalendarId,
                            (calendar, _) => new { calendar.Id, calendar.Name, calendar.HtmlColorCode }).OrderBy(x => x.Name).ToList();

                        tempOtherCalendars.AddRange(temporaryOtherCalendars.Select(tempOtherCalendar => new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));
                    }

                    var tempInputCalendars = calendarInputViewModel?.Calendars ?? [];

                    foreach (var tempInputCalendar in tempInputCalendars)
                    {
                        if ((tempCalendars.FirstOrDefault(x => x.Id == tempInputCalendar.Id) ?? new CalendarResponse())
                            .Id == 0 &&
                            (tempOtherCalendars.FirstOrDefault(x => x.Id == tempInputCalendar.Id) ??
                             new CalendarResponse()).Id == 0) continue;
                        {
                            foreach (var calendarEvent in await calendarService.GetCalendarEventsAsync(tempInputCalendar.Id))
                            {
                                CalendarEventOutputViewModel item = new()
                                {
                                    Id = calendarEvent.Id,
                                    CalendarId = calendarEvent.CalendarId,
                                    Title = calendarEvent.Title,
                                    AllDay = calendarEvent.AllDay
                                };

                                if (calendarEvent.AllDay)
                                {
                                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                                    {
                                        var systemTimeZoneIanaId = string.Empty;

                                        if (systemTimeZone.HasIanaId)
                                        {
                                            systemTimeZoneIanaId = systemTimeZone.Id;
                                        }
                                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                                        {
                                            systemTimeZoneIanaId = ianaId;
                                        }

                                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                        {
                                        }
                                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                                        {
                                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayStartDateTimeZone = string.Empty;
                                            break;
                                        }
                                    }

                                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                                    {
                                        var systemTimeZoneIanaId = string.Empty;

                                        if (systemTimeZone.HasIanaId)
                                        {
                                            systemTimeZoneIanaId = systemTimeZone.Id;
                                        }
                                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                                        {
                                            systemTimeZoneIanaId = ianaId;
                                        }

                                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                        {
                                        }
                                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                                        {
                                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayEndDateTimeZone = string.Empty;
                                            break;
                                        }
                                    }

                                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd");
                                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd");

                                    item.StartDate = calendarEvent.StartDate;
                                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                                }
                                else
                                {
                                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                                    {
                                        var systemTimeZoneIanaId = string.Empty;

                                        if (systemTimeZone.HasIanaId)
                                        {
                                            systemTimeZoneIanaId = systemTimeZone.Id;
                                        }
                                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                                        {
                                            systemTimeZoneIanaId = ianaId;
                                        }

                                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                        {
                                        }
                                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                                        {
                                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
                                            break;
                                        }
                                    }

                                    foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
                                    {
                                        var systemTimeZoneIanaId = string.Empty;

                                        if (systemTimeZone.HasIanaId)
                                        {
                                            systemTimeZoneIanaId = systemTimeZone.Id;
                                        }
                                        else if (TimeZoneInfo.TryConvertWindowsIdToIanaId(systemTimeZone.Id, out var ianaId))
                                        {
                                            systemTimeZoneIanaId = ianaId;
                                        }

                                        if (string.IsNullOrEmpty(systemTimeZoneIanaId))
                                        {
                                        }
                                        else if (systemTimeZoneIanaId == calendarEvent.EndDateTimeZoneIanaId)
                                        {
                                            calendarEvent.EndDate = TimeZoneInfo.ConvertTime(calendarEvent.EndDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayEndDateTimeZone = systemTimeZone.StandardName;
                                            break;
                                        }
                                    }

                                    item.DisplayStartDate = calendarEvent.StartDate.ToString("yyyy-MM-dd HH:mm");
                                    item.DisplayEndDate = calendarEvent.EndDate.ToString("yyyy-MM-dd HH:mm");

                                    item.StartDate = calendarEvent.StartDate;
                                    var tempEndDate = calendarEvent.EndDate.AddDays(1);
                                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                                }

                                if ((tempCalendars.FirstOrDefault(x => x.Id == tempInputCalendar.Id) ?? new CalendarResponse()).Id != 0)
                                {
                                    item.HtmlColorCode = tempCalendars.FirstOrDefault(x => x.Id == calendarEvent.CalendarId)?.HtmlColorCode;
                                    item.CalendarType = EnumHelper.GetDescription(CalendarType.My);
                                }
                                else if ((tempOtherCalendars.FirstOrDefault(x => x.Id == tempInputCalendar.Id) ?? new CalendarResponse()).Id != 0)
                                {
                                    item.HtmlColorCode = tempOtherCalendars.FirstOrDefault(x => x.Id == calendarEvent.CalendarId)?.HtmlColorCode;
                                    item.CalendarType = EnumHelper.GetDescription(CalendarType.Other);
                                }

                                calendarEvents.Add(item);
                            }
                        }
                    }

                    return calendarEvents.Count > 0
                        ? Json(new { result = true, calendarEvents = JsonConvert.SerializeObject(calendarEvents) })
                        : (IActionResult)Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns the list of calendar-sharing records as JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> GetCalendarShareds()
    {
        try
        {
            var calendars = (await calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty)).ToList();

            foreach (var calendar in calendars)
            {
                await calendarService.EnsureCalendarSharedAsync(calendar.Id);
            }

            List<CalendarSharedResponse> tempCalendarShareds = [];
            tempCalendarShareds.AddRange(((await calendarService.GetCalendarSharedAsync()).ToList())
                .Select(calendarShared => new
                {
                    calendarShared,
                    item = calendars.FirstOrDefault(x => x.Id == calendarShared.CalendarId) ??
                           new CalendarResponse()
                })
                .Where(@t => @t.item.Id > 0)
                .Select(@t => @t.calendarShared));

            var setCalendarShareds = calendars.Join(tempCalendarShareds, calendar => calendar.Id,
                sharedCalendar => sharedCalendar.CalendarId,
                (calendar, sharedCalendar) => new
                {
                    Id = sharedCalendar.CalendarId,
                    calendar.Name,
                    sharedCalendar.User,
                    Guest = sharedCalendar.Anonymous
                }).OrderBy(x => x.Name).ToList();


            return Json(new { result = true, setCalendarShareds });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns the list of shared/public calendars the logged-in user may browse.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetBrowseCalendarsOfInterest()
    {
        try
        {
            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (isAccountSessionExist)
            {
                var loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User)) // If not logged-in user
                {
                    return Json(new { result = false, error = localizer["Login required."].Value });
                }

                var allCalendars = await calendarService.GetAllCalendarsAsync();
                var calendarShareds = await calendarService.GetCalendarSharedAsync();
                var otherCalendars = await calendarService.GetOtherCalendarsAsync(_sessionService.GetAccount()!.Email ?? string.Empty);

                switch (loginedAccount.Role)
                {
                    case Role.User:
                        calendarShareds = calendarShareds.Where(x => x.User).ToList();
                        break;
                    default:
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                var browseCalendarsOfInterests = allCalendars
                    .Join(calendarShareds, calendar => calendar.Id, calendarShared => calendarShared.CalendarId,
                        (calendar, calendarShared) => new { calendar, calendarShared })
                    .GroupJoin(otherCalendars, @t => @t.calendar.Id, otherCalendar => otherCalendar.CalendarId,
                        (@t, otherCalendarGroup) => new { @t, otherCalendarGroup })
                    .SelectMany(@t => @t.otherCalendarGroup.DefaultIfEmpty(), (@t, otherCalendar) => new
                    {
                        @t.@t.calendar.Id,
                        @t.@t.calendar.Name,
                        Checked = otherCalendar != null // true if otherCalendar exists, false otherwise
                    }).OrderBy(x => x.Name).ToList();


                return Json(new { result = true, browseCalendarsOfInterests });
            }

            return Json(new { result = false, error = localizer["Please Login to update calendar event"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Update

    #region Calendar
    /// <summary>Updates an existing calendar's properties.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
            try
            {
                var calendarRequest = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new CalendarRequest();
                calendarRequest.AccountEmail = _sessionService.GetAccount()!.Email ?? string.Empty;

                var updateResult = await calendarService.UpdateCalendarAsync(calendarRequest);

                return !updateResult.Success ? Json(new { result = false, error = localizer[updateResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar has been successfully updated."].Value, calendar = new CalendarRequest() { Id = calendarRequest.Id, Name = calendarRequest.Name, HtmlColorCode = calendarRequest.HtmlColorCode } });
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Calendar Event
    /// <summary>Updates an existing calendar event (with optional file attachment and reminder changes).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateCalendarEvent([FromForm] CalendarEventInputViewModel calendarEventInputViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                calendarEventInputViewModel.Description ??= ""; // If no content was entered

                #region HtmlSanitizer
                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                var sanitized = sanitizer.Sanitize(calendarEventInputViewModel.Description);
                calendarEventInputViewModel.Description = sanitized;
                #endregion

                #region Decrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(calendarEventInputViewModel.Description);

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                {
                    var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", rsa.Decrypt(encryptedFilePath));
                }

                calendarEventInputViewModel.Description = htmlDocument.DocumentNode.OuterHtml;
                #endregion

                if (calendarEventInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = localizer["Name length is must be between 1 and 100 characters."].Value });
                }

                if (calendarEventInputViewModel.Description.Length > 16384) // Description Maxlength: 16KB
                {
                    return Json(new { result = false, error = localizer["Description length is must be between 0 and 16384 characters."].Value });
                }

                if (calendarEventInputViewModel.CalendarEventUploadedFile != null) // If a file is attached
                {
                    if (Path.GetExtension(calendarEventInputViewModel.CalendarEventUploadedFile.FileName) != ".zip") // Only zip extension allowed
                    {
                        return Json(new { result = false, error = localizer["Only zip extension allowed."].Value });
                    }

                    if (calendarEventInputViewModel.CalendarEventUploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }

                var isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = localizer["Please Login to update calendar event"].Value });

                var loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = localizer["Please Login to update calendar event"].Value });

                #region Build CalendarEventRequest with UTC dates
                CalendarEventRequest calendarEventRequest = new() { Id = calendarEventInputViewModel.Id };

                if (calendarEventInputViewModel.AllDay)
                {
                    calendarEventRequest.StartDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[0]),
                        Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[1]),
                        Convert.ToInt32(calendarEventInputViewModel.StartDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);
                    calendarEventRequest.EndDate = new DateTime(Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[0]),
                        Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[1]),
                        Convert.ToInt32(calendarEventInputViewModel.EndDate.Split("-")[2]), 0, 0, 0, DateTimeKind.Utc);
                    calendarEventRequest.StartDateTimeZoneIanaId = null;
                    calendarEventRequest.EndDateTimeZoneIanaId = null;
                }
                else
                {
                    var temporaryStartDate = calendarEventInputViewModel.StartDate.Split(" ");
                    var temporaryEndDate = calendarEventInputViewModel.EndDate.Split(" ");
                    DateTime tempStartDate = new(Convert.ToInt32(temporaryStartDate[0].Split("-")[0]),
                        Convert.ToInt32(temporaryStartDate[0].Split("-")[1]),
                        Convert.ToInt32(temporaryStartDate[0].Split("-")[2]),
                        Convert.ToInt32(temporaryStartDate[1].Split(":")[0]),
                        Convert.ToInt32(temporaryStartDate[1].Split(":")[1]),
                        0, DateTimeKind.Unspecified);
                    DateTime tempEndDate = new(Convert.ToInt32(temporaryEndDate[0].Split("-")[0]),
                        Convert.ToInt32(temporaryEndDate[0].Split("-")[1]),
                        Convert.ToInt32(temporaryEndDate[0].Split("-")[2]),
                        Convert.ToInt32(temporaryEndDate[1].Split(":")[0]),
                        Convert.ToInt32(temporaryEndDate[1].Split(":")[1]),
                        0, DateTimeKind.Unspecified);

                    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out var id) ? id : string.Empty);
                        if (string.IsNullOrEmpty(tzId) || tzId != calendarEventInputViewModel.StartDateTimeZoneIanaId)
                            continue;
                        tempStartDate = TimeZoneInfo.ConvertTime(tempStartDate, tz, TimeZoneInfo.Utc); break;
                    }
                    foreach (var tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        var tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out var id) ? id : string.Empty);
                        if (string.IsNullOrEmpty(tzId) || tzId != calendarEventInputViewModel.EndDateTimeZoneIanaId)
                            continue;
                        tempEndDate = TimeZoneInfo.ConvertTime(tempEndDate, tz, TimeZoneInfo.Utc); break;
                    }

                    calendarEventRequest.StartDate = tempStartDate;
                    calendarEventRequest.EndDate = tempEndDate;
                    calendarEventRequest.StartDateTimeZoneIanaId = calendarEventInputViewModel.StartDateTimeZoneIanaId;
                    calendarEventRequest.EndDateTimeZoneIanaId = calendarEventInputViewModel.EndDateTimeZoneIanaId;
                }

                calendarEventRequest.CalendarId = calendarEventInputViewModel.CalendarId;
                calendarEventRequest.Title = calendarEventInputViewModel.Title;
                calendarEventRequest.Description = calendarEventInputViewModel.Description;
                calendarEventRequest.AllDay = calendarEventInputViewModel.AllDay;
                calendarEventRequest.Location = calendarEventInputViewModel.Location;
                calendarEventRequest.Status = calendarEventInputViewModel.Status;
                #endregion

                #region Build reminders
                List<CalendarReminderInfo> reminders = [];
                var calendarReminders = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(calendarEventInputViewModel.SerializedCalendarReminders);
                reminders.AddRange(calendarReminders
                    .Select(reminder => new { reminder, tempTimes = reminder["TimesBeforeEvent"]?.ToString() })
                    .Select(@t => new CalendarReminderInfo
                    {
                        Method = @t.reminder["Method"].ToString() ?? string.Empty,
                        MinutesBeforeEvent =
                            @t.reminder["MinutesBeforeEvent"] != null
                                ? Convert.ToInt32(@t.reminder["MinutesBeforeEvent"])
                                : null,
                        HoursBeforeEvent =
                            @t.reminder["HoursBeforeEvent"] != null
                                ? Convert.ToInt32(@t.reminder["HoursBeforeEvent"])
                                : null,
                        DaysBeforeEvent =
                            @t.reminder["DaysBeforeEvent"] != null
                                ? Convert.ToInt32(@t.reminder["DaysBeforeEvent"])
                                : null,
                        WeeksBeforeEvent =
                            @t.reminder["WeeksBeforeEvent"] != null
                                ? Convert.ToInt32(@t.reminder["WeeksBeforeEvent"])
                                : null,
                        TimesBeforeEvent = !string.IsNullOrEmpty(@t.tempTimes)
                            ? new TimeSpan(Convert.ToInt32(@t.tempTimes.Split(":")[0]),
                                Convert.ToInt32(@t.tempTimes.Split(":")[1]), 0)
                            : null
                    }));

                #endregion

                #region Build attached file
                AttachedFileInfo attachedFile = null;
                if (calendarEventInputViewModel.CalendarEventUploadedFile != null)
                {
                    using MemoryStream ms = new();
                    await calendarEventInputViewModel.CalendarEventUploadedFile.CopyToAsync(ms);
                    attachedFile = new AttachedFileInfo
                    {
                        Bytes = ms.ToArray(),
                        ContentType = calendarEventInputViewModel.CalendarEventUploadedFile.ContentType,
                        FileName = calendarEventInputViewModel.CalendarEventUploadedFile.FileName,
                        Size = calendarEventInputViewModel.CalendarEventUploadedFile.Length
                    };
                }
                #endregion

                var updateEventResult = await calendarService.UpdateCalendarEventAsync(
                    calendarEventRequest,
                    loginedAccount.Email ?? string.Empty,
                    $"{loginedAccount.Role}Index",
                    reminders,
                    attachedFile);

                return !updateEventResult.Success ? Json(new { result = false, error = localizer[updateEventResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar event has been successfully updated."].Value });
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Calendar Shared
    /// <summary>Updates an existing CalendarShared record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateCalendarShared([FromBody] IEnumerable<CalendarSharedResponse> calendarShareds)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
            var sharedResult = await calendarService.UpdateCalendarSharedAsync(calendarShareds.Select(c => c.Adapt<CalendarSharedRequest>()));

            return !sharedResult.Success ? Json(new { result = false, error = localizer[sharedResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar shared has been successfully updated."].Value });

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Other Calendar
    /// <summary>Updates an existing OtherCalendar record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateOtherCalendar([FromBody] IEnumerable<OtherCalendarResponse> otherCalendars)
    {
        try
        {
            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
            var isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Login required."].Value });

            var loginedAccount = _sessionService.GetAccount()!;

            if (loginedAccount.Role is not (Role.Admin or Role.User))
                return Json(new { result = false, error = localizer["Login required."].Value });

            var otherCalendarResult = await calendarService.UpdateOtherCalendarAsync(
                loginedAccount.Email ?? string.Empty,
                otherCalendars.Select(c => c.Adapt<OtherCalendarRequest>()));

            return !otherCalendarResult.Success ? Json(new { result = false, error = localizer[otherCalendarResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The other calendar has been successfully updated."].Value });

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Delete
    /// <summary>Deletes an existing Calendar record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            var tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email;

            var deleteCalendarResult = await calendarService.DeleteCalendarAsync(tempCalendar);

            return !deleteCalendarResult.Success ? Json(new { result = false, error = localizer[deleteCalendarResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar has been successfully deleted."].Value, calendar = new CalendarRequest() { Id = tempCalendar.Id } });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Deletes an existing CalendarEvent record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteCalendarEvent(int id)
    {
        try
        {
            var email = _sessionService.GetAccount()!.Email;

            var deleteEventResult = await calendarService.DeleteCalendarEventAsync(id, email ?? string.Empty);

            return !deleteEventResult.Success ? Json(new { result = false, error = localizer[deleteEventResult.ErrorKey].Value }) : Json(new { result = true, message = localizer["The calendar event has been successfully deleted."].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion
}
