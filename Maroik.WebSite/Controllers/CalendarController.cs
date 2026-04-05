using Mapster;
﻿using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Calendar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class CalendarController : BaseController
{
    private readonly IRSAService _rsa;
    private readonly IHtmlLocalizer<CalendarController> _localizer;
    private readonly ILogger<CalendarController> _logger;
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly ICalendarRepository _calendarRepository;
    private readonly IOtherCalendarRepository _otherCalendarRepository;
    private readonly ICalendarEventRepository _calendarEventRepository;
    private readonly ICalendarEventAttachedFileRepository _calendarEventAttachedFileRepository;
    private readonly ICalendarEventReminderRepository _calendarReminderRepository;
    private readonly ICalendarSharedRepository _calendarSharedRepository;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IFileClient _fileClient;
    private readonly ICalendarService _calendarService;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ISessionService _sessionService;

    public CalendarController(IHtmlLocalizer<CalendarController> localizer, ILogger<CalendarController> logger, IHostEnvironment hostEnvironment, ICalendarService calendarService, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IAssetRepository assetRepository, IAccountRepository accountRepository, ICalendarRepository calendarRepository, IOtherCalendarRepository otherCalendarRepository, ICalendarEventRepository calendarEventRepository, ICalendarEventAttachedFileRepository calendarEventAttachedFileRepository, ICalendarEventReminderRepository calendarReminderRepository, ICalendarSharedRepository calendarSharedRepository, IFileClient fileClient, IOptions<ServerSetting> settings, IRSAService rsa, ISessionService sessionService)
    {
        _settings = settings;
        _rsa = rsa;
        _sessionService = sessionService;
        _localizer = localizer;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _calendarService = calendarService;
        _incomeRepository = incomeRepository;
        _expenditureRepository = expenditureRepository;
        _assetRepository = assetRepository;
        _accountRepository = accountRepository;
        _calendarRepository = calendarRepository;
        _otherCalendarRepository = otherCalendarRepository;
        _calendarEventRepository = calendarEventRepository;
        _calendarEventAttachedFileRepository = calendarEventAttachedFileRepository;
        _calendarReminderRepository = calendarReminderRepository;
        _calendarSharedRepository = calendarSharedRepository;
        _fileClient = fileClient;
    }

    #region Calendar

    #region Create

    #region Calendar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarRequest tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email;

            ServiceResult result = await _calendarService.CreateCalendarAsync(tempCalendar);

            if (!result.Success)
                return Json(new { result = false, error = _localizer[result.ErrorKey ?? "Input is invalid"].Value });

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
    #endregion

    #region Calendar Event
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
                string sanitized = sanitizer.Sanitize(calendarEventInputViewModel.Description);
                calendarEventInputViewModel.Description = sanitized;
                #endregion

                #region Decrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(calendarEventInputViewModel.Description);

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                calendarEventInputViewModel.Description = htmlDocument.DocumentNode.OuterHtml;
                #endregion

                if (calendarEventInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = _localizer["Name length is must be between 1 and 100 characters."].Value });
                }

                if (calendarEventInputViewModel.Description.Length > 16384) // Description Maxlength: 16KB
                {
                    return Json(new { result = false, error = _localizer["Description length is must be between 0 and 16384 characters."].Value });
                }

                if (calendarEventInputViewModel.CalendarEventUploadedFile != null) // If a file is attached
                {
                    if (!(Path.GetExtension(calendarEventInputViewModel.CalendarEventUploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (calendarEventInputViewModel.CalendarEventUploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }

                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = _localizer["Please Login to create calendar event"].Value });

                AccountResponse loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = _localizer["Please Login to create calendar event"].Value });

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
                    string[] temporaryStartDate = calendarEventInputViewModel.StartDate.Split(" ");
                    string[] temporaryEndDate = calendarEventInputViewModel.EndDate.Split(" ");
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

                    foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        string tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string id) ? id : string.Empty);
                        if (!string.IsNullOrEmpty(tzId) && tzId == calendarEventInputViewModel.StartDateTimeZoneIanaId)
                        { tempStartDate = TimeZoneInfo.ConvertTime(tempStartDate, tz, TimeZoneInfo.Utc); break; }
                    }
                    foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        string tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string id) ? id : string.Empty);
                        if (!string.IsNullOrEmpty(tzId) && tzId == calendarEventInputViewModel.EndDateTimeZoneIanaId)
                        { tempEndDate = TimeZoneInfo.ConvertTime(tempEndDate, tz, TimeZoneInfo.Utc); break; }
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
                List<Dictionary<string, object>> calendarReminders = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(calendarEventInputViewModel.SerializedCalendarReminders);
                foreach (Dictionary<string, object> reminder in calendarReminders)
                {
                    string tempTimes = reminder["TimesBeforeEvent"]?.ToString();
                    reminders.Add(new CalendarReminderInfo
                    {
                        Method = reminder["Method"].ToString(),
                        MinutesBeforeEvent = reminder["MinutesBeforeEvent"] != null ? Convert.ToInt32(reminder["MinutesBeforeEvent"]) : null,
                        HoursBeforeEvent = reminder["HoursBeforeEvent"] != null ? Convert.ToInt32(reminder["HoursBeforeEvent"]) : null,
                        DaysBeforeEvent = reminder["DaysBeforeEvent"] != null ? Convert.ToInt32(reminder["DaysBeforeEvent"]) : null,
                        WeeksBeforeEvent = reminder["WeeksBeforeEvent"] != null ? Convert.ToInt32(reminder["WeeksBeforeEvent"]) : null,
                        TimesBeforeEvent = !string.IsNullOrEmpty(tempTimes)
                            ? new TimeSpan(Convert.ToInt32(tempTimes.Split(":")[0]), Convert.ToInt32(tempTimes.Split(":")[1]), 0)
                            : null
                    });
                }
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

                ServiceResult createEventResult = await _calendarService.CreateCalendarEventAsync(
                    calendarEventRequest,
                    loginedAccount.Email,
                    $"{loginedAccount.Role}Index",
                    reminders,
                    attachedFile);

                if (!createEventResult.Success)
                    return Json(new { result = false, error = _localizer[createEventResult.ErrorKey ?? "Input is invalid"].Value });

                return Json(new { result = true, message = _localizer["The calendar event has been successfully created."].Value });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
    {
        if (summernoteImageFile == null)
        {
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
        }

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
        }

        // Extension validation
        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg, .jpeg or .png file allowed."].Value });
        }

        try
        {
            // Check if it's a real image (Magick.NET)
            using var image = new MagickImage(summernoteImageFile.OpenReadStream());
            // If Magick.NET throws an exception, it's not an image or not an allowed format
        }
        catch (MagickException)
        {
            return Ok(new { result = false, errorMessage = _localizer["Invalid image file."].Value });
        }

        var loginedAccount = _sessionService.GetAccount()!;

        string summernoteImagePath = Path.Combine("upload", "Calendar", $"{loginedAccount.Role}Index", "summernote", "images");
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine(summernoteImagePath, imageFile);

        bool uploadResult;
        using (MemoryStream ms = new())
        {
            await summernoteImageFile.CopyToAsync(ms);
            uploadResult = await _fileClient.UploadAsync(ms.ToArray(), summernoteImageFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl);
        }

        if (uploadResult)
        {
            var fileStream = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl);
            return Ok(new
            {
                result = true,
                file = File(fileStream, summernoteImageFile.ContentType, imageFile),
                filePath = _rsa.Encrypt(filePath)
            });
        }
        else
        {
            return Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> AdminIndex()
    {
        AccountResponse loginedAccount = _sessionService.GetAccount()!;

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load Calendar
        List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(loginedAccount.Email) ?? [];

        if (calendars.Count == 0)
        {
            calendars.Add(new CalendarResponse
            {
                AccountEmail = loginedAccount.Email,
                Name = loginedAccount.Nickname,
                TimeZoneIanaId = loginedAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e", // Default Html Color Code
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
            await _calendarRepository.CreateCalendarAsync(calendars[0].Adapt<CalendarRequest>());
        }
        #endregion

        #region Load CalendarEvent
        List<CalendarEventOutputViewModel> calendarEventOutputViewModels = [];

        foreach (CalendarResponse calendar in calendars)
        {
            foreach (CalendarEventResponse calendarEvent in await _calendarEventRepository.GetCalendarEventsAsync(calendar.Id) ?? [])
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
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
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
                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
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
                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
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
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount?.TimeZoneIanaId ?? "UTC";
        {
            DateTime _ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = _ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = _ct.Hour;
            calendarOutputViewModel.CurrentMinute = _ct.Minute;
            calendarOutputViewModel.CurrentSecond = _ct.Second;
            List<string> _ti = [];
            for (int _h = 0; _h < 24; _h++)
                for (int _m = 0; _m < 60; _m += 15)
                    _ti.Add($"{_h:D2}:{_m:D2}");
            calendarOutputViewModel.TimeIntervals = _ti;
            int _nm = (int)Math.Round(_ct.Minute / 15.0) * 15;
            if (_nm == 60) { _ct = _ct.AddHours(1); _nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{_ct.Hour:D2}:{_nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> UserIndex()
    {
        AccountResponse loginedAccount = _sessionService.GetAccount()!;

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load Calendar
        List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(loginedAccount.Email) ?? [];

        if (calendars.Count == 0)
        {
            calendars.Add(new CalendarResponse
            {
                AccountEmail = loginedAccount.Email,
                Name = loginedAccount.Nickname,
                TimeZoneIanaId = loginedAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e", // Default Html Color Code
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            });
            await _calendarRepository.CreateCalendarAsync(calendars[0].Adapt<CalendarRequest>());
        }
        #endregion

        #region Load CalendarEvent
        List<CalendarEventOutputViewModel> calendarEventOutputViewModels = [];

        foreach (CalendarResponse calendar in calendars)
        {
            foreach (CalendarEventResponse calendarEvent in await _calendarEventRepository.GetCalendarEventsAsync(calendar.Id) ?? [])
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
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
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
                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
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
                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                        {
                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }

                item.HtmlColorCode = calendar.HtmlColorCode;

                calendarEventOutputViewModels.Add(item);
            }
        }
        #endregion

        #region Load OtherCalendar
        List<CalendarResponse> otherCalendars = [];

        List<CalendarResponse> allCalendars = await _calendarRepository.GetAllCalendarsAsync() ?? [];

        var tempOtherCalendars = (from calendar in allCalendars
                                  join otherCalendar in await _otherCalendarRepository.GetOtherCalendarsAsync(loginedAccount.Email) ?? []
                                  on calendar.Id equals otherCalendar.CalendarId
                                  select new
                                  {
                                      calendar.Id,
                                      calendar.Name,
                                      calendar.HtmlColorCode
                                  }).OrderBy(x => x.Name).ToList();

        foreach (var tempOtherCalendar in tempOtherCalendars)
        {
            otherCalendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
        }
        #endregion

        #region Load OtherCalendarEvent
        List<CalendarEventOutputViewModel> otherCalendarEventOutputViewModels = [];

        foreach (CalendarResponse otherCalendar in otherCalendars)
        {
            foreach (CalendarEventResponse otherCalendarEvent in await _calendarEventRepository.GetCalendarEventsAsync(otherCalendar.Id) ?? [])
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
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
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
                    DateTime tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
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
                        else if (systemTimeZoneIanaId == otherCalendarEvent.StartDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    DateTime tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
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
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount?.TimeZoneIanaId ?? "UTC";
        {
            DateTime _ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = _ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = _ct.Hour;
            calendarOutputViewModel.CurrentMinute = _ct.Minute;
            calendarOutputViewModel.CurrentSecond = _ct.Second;
            List<string> _ti = [];
            for (int _h = 0; _h < 24; _h++)
                for (int _m = 0; _m < 60; _m += 15)
                    _ti.Add($"{_h:D2}:{_m:D2}");
            calendarOutputViewModel.TimeIntervals = _ti;
            int _nm = (int)Math.Round(_ct.Minute / 15.0) * 15;
            if (_nm == 60) { _ct = _ct.AddHours(1); _nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{_ct.Hour:D2}:{_nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> AnonymousIndex()
    {
        AccountResponse loginedAccount = new() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };

        CalendarOutputViewModel calendarOutputViewModel = new();

        #region Load OtherCalendar
        List<CalendarResponse> otherCalendars = [];

        List<CalendarResponse> allCalendars = await _calendarRepository.GetAllCalendarsAsync() ?? [];

        var tempOtherCalendars = (from calendar in allCalendars
                                  join calendarShared in (await _calendarSharedRepository?.GetCalendarSharedAsync())?.Where(x => x.Anonymous)?.ToList() ?? []
                                  on calendar.Id equals calendarShared.CalendarId
                                  select new
                                  {
                                      calendar.Id,
                                      calendar.Name,
                                      calendar.HtmlColorCode
                                  }).OrderBy(x => x.Name).ToList();

        foreach (var tempOtherCalendar in tempOtherCalendars)
        {
            otherCalendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
        }
        #endregion

        #region Load OtherCalendarEvent
        List<CalendarEventOutputViewModel> otherCalendarEventOutputViewModels = [];

        foreach (CalendarResponse otherCalendar in otherCalendars)
        {
            foreach (CalendarEventResponse otherCalendarEvent in await _calendarEventRepository.GetCalendarEventsAsync(otherCalendar.Id) ?? [])
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
                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = string.Empty;
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
                    DateTime tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                }
                else
                {
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
                        else if (systemTimeZoneIanaId == otherCalendarEvent.StartDateTimeZoneIanaId)
                        {
                            otherCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(otherCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    DateTime tempEndDate = otherCalendarEvent.EndDate.AddDays(1);
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
        calendarOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount?.TimeZoneIanaId ?? "UTC";
        {
            DateTime _ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(calendarOutputViewModel.LoginedAccountTimeZoneIanaId);
            calendarOutputViewModel.CurrentDate = _ct.ToString("yyyy-MM-dd");
            calendarOutputViewModel.CurrentHour = _ct.Hour;
            calendarOutputViewModel.CurrentMinute = _ct.Minute;
            calendarOutputViewModel.CurrentSecond = _ct.Second;
            List<string> _ti = [];
            for (int _h = 0; _h < 24; _h++)
                for (int _m = 0; _m < 60; _m += 15)
                    _ti.Add($"{_h:D2}:{_m:D2}");
            calendarOutputViewModel.TimeIntervals = _ti;
            int _nm = (int)Math.Round(_ct.Minute / 15.0) * 15;
            if (_nm == 60) { _ct = _ct.AddHours(1); _nm = 0; }
            calendarOutputViewModel.CurrentInterval = $"{_ct.Hour:D2}:{_nm:D2}";
        }

        return View(calendarOutputViewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsCalendarExists(int id)
    {
        try
        {
            List<CalendarResponse> tempCalendars = await _calendarRepository.GetCalendarsAsync(_sessionService.GetAccount()!.Email) ?? [];

            if (tempCalendars == null || tempCalendars.Count == 0)
            {
                return Json(new { result = false, error = _localizer["No calendar exists"].Value });
            }
            else
            {
                CalendarResponse tempCalendar = tempCalendars.Where(a => a.Id == id).FirstOrDefault();

                return tempCalendar == null
                    ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                    : (IActionResult)Json(new { result = true, calendar = tempCalendar });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetCalendars()
    {
        try
        {
            List<CalendarResponse> calendars = (await _calendarRepository.GetCalendarsAsync(_sessionService.GetAccount()!.Email) ?? [])?.ToList();

            return Json(new { result = true, calendars });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> GetOtherCalendars()
    {
        try
        {
            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (isAccountSessionExist)
            {
                List<CalendarResponse> calendars = await _calendarRepository.GetAllCalendarsAsync();
                List<OtherCalendarResponse> otherCalendars = (await _otherCalendarRepository.GetOtherCalendarsAsync(_sessionService.GetAccount()!.Email) ?? [])?.ToList();

                List<CalendarResponse> tempOtherCalendars = (from calendar in calendars
                                                     join otherCalendar in otherCalendars
                                                     on calendar.Id equals otherCalendar.CalendarId
                                                     select new CalendarResponse()
                                                     {
                                                         Id = calendar.Id,
                                                         Name = calendar.Name,
                                                         HtmlColorCode = calendar.HtmlColorCode,
                                                     }).OrderBy(x => x.Name).ToList();

                return Json(new { result = true, tempOtherCalendars });
            }
            else
            {
                List<CalendarResponse> tempOtherCalendars = [];
                var temporaryOtherCalendars = (from calendar in await _calendarRepository.GetAllCalendarsAsync() ?? []
                                               join calendarShared in (await _calendarSharedRepository?.GetCalendarSharedAsync())?.Where(x => x.Anonymous)?.ToList() ?? []
                                               on calendar.Id equals calendarShared.CalendarId
                                               select new
                                               {
                                                   calendar.Id,
                                                   calendar.Name,
                                                   calendar.HtmlColorCode
                                               }).OrderBy(x => x.Name).ToList();

                foreach (var tempOtherCalendar in temporaryOtherCalendars)
                {
                    tempOtherCalendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
                }

                return Json(new { result = true, tempOtherCalendars });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

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
                    AccountResponse loginedAccount = _sessionService.GetAccount()!;

                    List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(loginedAccount.Email) ?? [];

                    CalendarEventResponse tempCalendarEvent = await _calendarEventRepository.GetCalendarEventAsync(id);
                    CalendarResponse tempCalendar = calendars?.Where(x => x.Id == tempCalendarEvent.CalendarId)?.FirstOrDefault() ?? new CalendarResponse();

                    if (!(tempCalendarEvent.Id > 0))
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    if (!(tempCalendar.Id > 0))
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    CalendarEventOutputViewModel calendarEvent = new()
                    {
                        Id = tempCalendarEvent.Id,
                        CalendarId = tempCalendarEvent.CalendarId,
                        Title = tempCalendarEvent.Title,
                        AllDay = tempCalendarEvent.AllDay
                    };

                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(tempCalendarEvent.Description);

                    foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                    {
                        string filePath = imgTag.GetAttributeValue("alt", "");

                        // Method to download file
                        byte[] fileData = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl);

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(filePath, out string contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            string base64Data = Convert.ToBase64String(fileData);

                            // Store base64 data in img tag (use data-file attribute instead of src)
                            _ = imgTag.SetAttributeValue("data-file", base64Data);
                            _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                            _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                        }
                    }

                    calendarEvent.Description = htmlDocument.DocumentNode.OuterHtml;

                    if (tempCalendarEvent.AllDay)
                    {
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
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = string.Empty;
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
                            else if (systemTimeZoneIanaId == tempCalendarEvent.StartDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    calendarEvent.CalendarEventAttachedFile = await _calendarEventAttachedFileRepository.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id);

                    if (!string.IsNullOrEmpty(calendarEvent?.CalendarEventAttachedFile?.Path ?? ""))
                    {
                        byte[] fileData = await _fileClient.DownloadAsync(calendarEvent?.CalendarEventAttachedFile?.Path ?? "", _settings.Value.FileStorageBaseUrl);

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(calendarEvent?.CalendarEventAttachedFile?.Path ?? "", out string contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                            calendarEvent.CalendarEventAttachedFileContentType = contentType;
                        }
                    }

                    calendarEvent.Calendars = calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name).ToList();
                    calendarEvent.SerializedCalendarReminders = JsonConvert.SerializeObject((await _calendarReminderRepository.GetCalendarEventRemindersAsync(tempCalendarEvent.Id)).Select(x => new CalendarEventReminder { Method = x.Method, MinutesBeforeEvent = x.MinutesBeforeEvent, HoursBeforeEvent = x.HoursBeforeEvent, DaysBeforeEvent = x.DaysBeforeEvent, WeeksBeforeEvent = x.WeeksBeforeEvent, TimesBeforeEvent = x.TimesBeforeEvent }) ?? []);

                    calendarEvent.HtmlColorCode = calendars?.Where(x => x.Id == tempCalendarEvent.CalendarId)?.FirstOrDefault()?.HtmlColorCode;

                    return Json(new { result = true, calendarEvent });
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
                    bool isAccountSessionExist = _sessionService.GetAccount() != null;
                    AccountResponse loginedAccount = null;
                    List<CalendarResponse> calendars = null;

                    if (isAccountSessionExist)
                    {
                        loginedAccount = _sessionService.GetAccount()!;
                        calendars = (from calendar in await _calendarRepository.GetAllCalendarsAsync() ?? []
                                     join otherCalendar in await _otherCalendarRepository.GetOtherCalendarsAsync(loginedAccount.Email) ?? []
                                     on calendar.Id equals otherCalendar.CalendarId
                                     select calendar).ToList();
                    }
                    else
                    {
                        loginedAccount = new AccountResponse() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
                        calendars = [];
                        var temporaryOtherCalendars = (from calendar in await _calendarRepository.GetAllCalendarsAsync() ?? []
                                                       join calendarShared in (await _calendarSharedRepository?.GetCalendarSharedAsync())?.Where(x => x.Anonymous)?.ToList() ?? []
                                                       on calendar.Id equals calendarShared.CalendarId
                                                       select new
                                                       {
                                                           calendar.Id,
                                                           calendar.Name,
                                                           calendar.HtmlColorCode
                                                       }).OrderBy(x => x.Name).ToList();

                        foreach (var tempOtherCalendar in temporaryOtherCalendars)
                        {
                            calendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
                        }
                    }

                    CalendarEventResponse tempCalendarEvent = await _calendarEventRepository.GetCalendarEventAsync(id);
                    CalendarResponse tempCalendar = calendars?.Where(x => x.Id == tempCalendarEvent.CalendarId)?.FirstOrDefault() ?? new CalendarResponse();

                    if (!(tempCalendarEvent.Id > 0))
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    if (!(tempCalendar.Id > 0))
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    CalendarEventOutputViewModel calendarEvent = new()
                    {
                        Id = tempCalendarEvent.Id,
                        CalendarId = tempCalendarEvent.CalendarId,
                        Title = tempCalendarEvent.Title,
                        AllDay = tempCalendarEvent.AllDay
                    };

                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(tempCalendarEvent.Description);

                    foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                    {
                        string filePath = imgTag.GetAttributeValue("alt", "");

                        // Method to download file
                        byte[] fileData = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl);

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(filePath, out string contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            string base64Data = Convert.ToBase64String(fileData);

                            // Store base64 data in img tag (use data-file attribute instead of src)
                            _ = imgTag.SetAttributeValue("data-file", base64Data);
                            _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                            _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                        }
                    }

                    calendarEvent.Description = htmlDocument.DocumentNode.OuterHtml;

                    if (tempCalendarEvent.AllDay)
                    {
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
                            else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = string.Empty;
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
                            else if (systemTimeZoneIanaId == tempCalendarEvent.StartDateTimeZoneIanaId)
                            {
                                tempCalendarEvent.StartDate = TimeZoneInfo.ConvertTime(tempCalendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                calendarEvent.StartDateTimeZoneIanaId = systemTimeZoneIanaId;
                                calendarEvent.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                    calendarEvent.CalendarEventAttachedFile = await _calendarEventAttachedFileRepository.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id);

                    if (!string.IsNullOrEmpty(calendarEvent?.CalendarEventAttachedFile?.Path ?? ""))
                    {
                        byte[] fileData = await _fileClient.DownloadAsync(calendarEvent?.CalendarEventAttachedFile?.Path ?? "", _settings.Value.FileStorageBaseUrl);

                        if (fileData != null)
                        {
                            // Use FileExtensionContentTypeProvider to extract ContentType
                            FileExtensionContentTypeProvider provider = new();

                            // Default contentType is set to "application/octet-stream"
                            if (!provider.TryGetContentType(calendarEvent?.CalendarEventAttachedFile?.Path ?? "", out string contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // Convert file data to Base64
                            calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                            calendarEvent.CalendarEventAttachedFileContentType = contentType;
                        }
                    }

                    calendarEvent.Calendars = calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name).ToList();
                    calendarEvent.SerializedCalendarReminders = JsonConvert.SerializeObject((await _calendarReminderRepository.GetCalendarEventRemindersAsync(tempCalendarEvent.Id)).Select(x => new CalendarEventReminder { Method = x.Method, MinutesBeforeEvent = x.MinutesBeforeEvent, HoursBeforeEvent = x.HoursBeforeEvent, DaysBeforeEvent = x.DaysBeforeEvent, WeeksBeforeEvent = x.WeeksBeforeEvent, TimesBeforeEvent = x.TimesBeforeEvent }) ?? []);

                    calendarEvent.HtmlColorCode = calendars?.Where(x => x.Id == tempCalendarEvent.CalendarId)?.FirstOrDefault()?.HtmlColorCode;

                    return Json(new { result = true, calendarEvent });
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

                    bool isAccountSessionExist = _sessionService.GetAccount() != null;
                    AccountResponse loginedAccount = null;

                    List<CalendarResponse> tempCalendars = null;

                    List<CalendarResponse> tempOtherCalendars = null;

                    if (isAccountSessionExist)
                    {
                        loginedAccount = _sessionService.GetAccount()!;

                        tempCalendars = await _calendarRepository.GetCalendarsAsync(_sessionService.GetAccount()!.Email);

                        tempOtherCalendars = [];

                        var otherCalendars = (from calendar in await _calendarRepository.GetAllCalendarsAsync() ?? []
                                              join otherCalendar in await _otherCalendarRepository.GetOtherCalendarsAsync(loginedAccount.Email) ?? []
                                              on calendar.Id equals otherCalendar.CalendarId
                                              select new
                                              {
                                                  calendar.Id,
                                                  calendar.Name,
                                                  calendar.HtmlColorCode
                                              }).OrderBy(x => x.Name).ToList();

                        foreach (var tempOtherCalendar in otherCalendars)
                        {
                            tempOtherCalendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
                        }
                    }
                    else
                    {
                        loginedAccount = new AccountResponse() { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };

                        tempCalendars = [];

                        tempOtherCalendars = [];

                        var temporaryOtherCalendars = (from calendar in await _calendarRepository.GetAllCalendarsAsync() ?? []
                                                       join calendarShared in (await _calendarSharedRepository?.GetCalendarSharedAsync())?.Where(x => x.Anonymous)?.ToList() ?? []
                                                       on calendar.Id equals calendarShared.CalendarId
                                                       select new
                                                       {
                                                           calendar.Id,
                                                           calendar.Name,
                                                           calendar.HtmlColorCode
                                                       }).OrderBy(x => x.Name).ToList();

                        foreach (var tempOtherCalendar in temporaryOtherCalendars)
                        {
                            tempOtherCalendars.Add(new CalendarResponse() { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode });
                        }
                    }

                    IEnumerable<CalendarRequest> tempInputCalendars = calendarInputViewModel?.Calendars ?? [];

                    foreach (CalendarRequest tempInputCalendar in tempInputCalendars)
                    {
                        if (((tempCalendars?.Where(x => x.Id == tempInputCalendar.Id)?.FirstOrDefault() ?? new CalendarResponse()).Id != 0) ||
                            ((tempOtherCalendars?.Where(x => x.Id == tempInputCalendar.Id)?.FirstOrDefault() ?? new CalendarResponse()).Id != 0))
                        {
                            foreach (CalendarEventResponse calendarEvent in await _calendarEventRepository.GetCalendarEventsAsync(tempInputCalendar.Id))
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
                                        else if (systemTimeZoneIanaId == loginedAccount.TimeZoneIanaId)
                                        {
                                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayStartDateTimeZone = string.Empty;
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
                                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
                                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                                }
                                else
                                {
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
                                        else if (systemTimeZoneIanaId == calendarEvent.StartDateTimeZoneIanaId)
                                        {
                                            calendarEvent.StartDate = TimeZoneInfo.ConvertTime(calendarEvent.StartDate, TimeZoneInfo.Utc, systemTimeZone);
                                            item.DisplayStartDateTimeZone = systemTimeZone.StandardName;
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
                                    DateTime tempEndDate = calendarEvent.EndDate.AddDays(1);
                                    item.EndDate = new DateTime(tempEndDate.Year, tempEndDate.Month, tempEndDate.Day, 0, 0, 0, tempEndDate.Kind);
                                }

                                if ((tempCalendars?.Where(x => x.Id == tempInputCalendar.Id)?.FirstOrDefault() ?? new CalendarResponse()).Id != 0)
                                {
                                    item.HtmlColorCode = tempCalendars?.Where(x => x.Id == calendarEvent.CalendarId)?.FirstOrDefault()?.HtmlColorCode;
                                    item.CalendarType = EnumHelper.GetDescription(CalendarType.My);
                                }
                                else if ((tempOtherCalendars?.Where(x => x.Id == tempInputCalendar.Id)?.FirstOrDefault() ?? new CalendarResponse()).Id != 0)
                                {
                                    item.HtmlColorCode = tempOtherCalendars?.Where(x => x.Id == calendarEvent.CalendarId)?.FirstOrDefault()?.HtmlColorCode;
                                    item.CalendarType = EnumHelper.GetDescription(CalendarType.Other);
                                }

                                calendarEvents.Add(item);
                            }
                        }
                    }

                    return calendarEvents.Count > 0
                        ? Json(new { result = true, calendarEvents = JsonConvert.SerializeObject(calendarEvents) })
                        : (IActionResult)Json(new { result = false, error = _localizer["Input is invalid"].Value });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> GetCalendarShareds()
    {
        try
        {
            List<CalendarResponse> calendars = (await _calendarRepository.GetCalendarsAsync(_sessionService.GetAccount()!.Email) ?? [])?.ToList();
            List<CalendarSharedResponse> calendarShareds = (await _calendarSharedRepository.GetCalendarSharedAsync() ?? [])?.ToList();

            foreach (CalendarResponse calendar in calendars)
            {
                CalendarSharedResponse calendarShared = calendarShareds?.Where(x => x.CalendarId == calendar.Id)?.FirstOrDefault() ?? new CalendarSharedResponse();

                if (!(calendarShared.CalendarId > 0))
                {
                    await _calendarSharedRepository.CreateCalendarSharedAsync(new CalendarSharedRequest() { CalendarId = calendar.Id, User = false, Anonymous = false });
                }
            }

            List<CalendarSharedResponse> tempCalendarShareds = [];

            foreach (CalendarSharedResponse calendarShared in (await _calendarSharedRepository.GetCalendarSharedAsync() ?? [])?.ToList())
            {
                CalendarResponse item = calendars.Where(x => x.Id == calendarShared.CalendarId).FirstOrDefault() ?? new CalendarResponse();

                if (item.Id > 0)
                {
                    tempCalendarShareds.Add(calendarShared);
                }
            }

            var setCalendarShareds = (from calendar in calendars
                                      join sharedCalendar in tempCalendarShareds
                                      on calendar.Id equals sharedCalendar.CalendarId
                                      select new
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
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetBrowseCalendarsOfInterest()
    {
        try
        {
            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (isAccountSessionExist)
            {
                AccountResponse loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User)) // If not logged in user
                {
                    return Json(new { result = false, error = _localizer["Login required."].Value });
                }

                List<CalendarResponse> allCalendars = await _calendarRepository.GetAllCalendarsAsync() ?? [];
                List<CalendarSharedResponse> calendarShareds = await _calendarSharedRepository.GetCalendarSharedAsync() ?? [];
                List<OtherCalendarResponse> otherCalendars = await _otherCalendarRepository.GetOtherCalendarsAsync(_sessionService.GetAccount()!.Email);

                switch (loginedAccount.Role)
                {
                    case Role.User:
                        calendarShareds = calendarShareds.Where(x => x.User).ToList();
                        break;
                    default:
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }

                var browseCalendarsOfInterests = (from calendar in allCalendars
                                                  join calendarShared in calendarShareds
                                                  on calendar.Id equals calendarShared.CalendarId
                                                  // Left Join otherCalendars based on calendar.Id
                                                  join otherCalendar in otherCalendars
                                                  on calendar.Id equals otherCalendar.CalendarId into otherCalendarGroup
                                                  from otherCalendar in otherCalendarGroup.DefaultIfEmpty() // Left Join
                                                  select new
                                                  {
                                                      calendar.Id,
                                                      calendar.Name,
                                                      Checked = otherCalendar != null // true if otherCalendar exists, false otherwise
                                                  }).OrderBy(x => x.Name).ToList();


                return Json(new { result = true, browseCalendarsOfInterests });
            }
            else
            {
                return Json(new { result = false, error = _localizer["Please Login to update calendar event"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Update

    #region Calendar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    CalendarResponse tempCalendar = (calendarInputViewModel?.Calendars?.FirstOrDefault())?.Adapt<CalendarResponse>() ?? new CalendarResponse();
                    tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email;
                    tempCalendar.Updated = DateTime.UtcNow;

                    List<CalendarResponse> tempCalendars = await _calendarRepository.GetCalendarsAsync(tempCalendar.AccountEmail);
                    CalendarResponse previousCalendar = tempCalendars.Where(a => a.Id == tempCalendar.Id).FirstOrDefault();
                    tempCalendar.Created = previousCalendar.Created;

                    if (previousCalendar.Id > 0)
                    {
                        await _calendarRepository.UpdateCalendarAsync(tempCalendar.Adapt<CalendarRequest>());

                        return Json(new { result = true, message = _localizer["The calendar has been successfully updated."].Value, calendar = new CalendarRequest() { Id = tempCalendar.Id, Name = tempCalendar.Name, HtmlColorCode = tempCalendar.HtmlColorCode } });
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

    #region Calendar Event
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
                string sanitized = sanitizer.Sanitize(calendarEventInputViewModel.Description);
                calendarEventInputViewModel.Description = sanitized;
                #endregion

                #region Decrypt img tag file path
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(calendarEventInputViewModel.Description);

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                calendarEventInputViewModel.Description = htmlDocument.DocumentNode.OuterHtml;
                #endregion

                if (calendarEventInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new { result = false, error = _localizer["Name length is must be between 1 and 100 characters."].Value });
                }

                if (calendarEventInputViewModel.Description.Length > 16384) // Description Maxlength: 16KB
                {
                    return Json(new { result = false, error = _localizer["Description length is must be between 0 and 16384 characters."].Value });
                }

                if (calendarEventInputViewModel.CalendarEventUploadedFile != null) // If a file is attached
                {
                    if (!(Path.GetExtension(calendarEventInputViewModel.CalendarEventUploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (calendarEventInputViewModel.CalendarEventUploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                    }
                }

                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = _localizer["Please Login to update calendar event"].Value });

                AccountResponse loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = _localizer["Please Login to update calendar event"].Value });

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
                    string[] temporaryStartDate = calendarEventInputViewModel.StartDate.Split(" ");
                    string[] temporaryEndDate = calendarEventInputViewModel.EndDate.Split(" ");
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

                    foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        string tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string id) ? id : string.Empty);
                        if (!string.IsNullOrEmpty(tzId) && tzId == calendarEventInputViewModel.StartDateTimeZoneIanaId)
                        { tempStartDate = TimeZoneInfo.ConvertTime(tempStartDate, tz, TimeZoneInfo.Utc); break; }
                    }
                    foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
                    {
                        string tzId = tz.HasIanaId ? tz.Id : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string id) ? id : string.Empty);
                        if (!string.IsNullOrEmpty(tzId) && tzId == calendarEventInputViewModel.EndDateTimeZoneIanaId)
                        { tempEndDate = TimeZoneInfo.ConvertTime(tempEndDate, tz, TimeZoneInfo.Utc); break; }
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
                List<Dictionary<string, object>> calendarReminders = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(calendarEventInputViewModel.SerializedCalendarReminders);
                foreach (Dictionary<string, object> reminder in calendarReminders)
                {
                    string tempTimes = reminder["TimesBeforeEvent"]?.ToString();
                    reminders.Add(new CalendarReminderInfo
                    {
                        Method = reminder["Method"].ToString(),
                        MinutesBeforeEvent = reminder["MinutesBeforeEvent"] != null ? Convert.ToInt32(reminder["MinutesBeforeEvent"]) : null,
                        HoursBeforeEvent = reminder["HoursBeforeEvent"] != null ? Convert.ToInt32(reminder["HoursBeforeEvent"]) : null,
                        DaysBeforeEvent = reminder["DaysBeforeEvent"] != null ? Convert.ToInt32(reminder["DaysBeforeEvent"]) : null,
                        WeeksBeforeEvent = reminder["WeeksBeforeEvent"] != null ? Convert.ToInt32(reminder["WeeksBeforeEvent"]) : null,
                        TimesBeforeEvent = !string.IsNullOrEmpty(tempTimes)
                            ? new TimeSpan(Convert.ToInt32(tempTimes.Split(":")[0]), Convert.ToInt32(tempTimes.Split(":")[1]), 0)
                            : null
                    });
                }
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

                ServiceResult updateEventResult = await _calendarService.UpdateCalendarEventAsync(
                    calendarEventRequest,
                    loginedAccount.Email,
                    $"{loginedAccount.Role}Index",
                    reminders,
                    attachedFile);

                if (!updateEventResult.Success)
                    return Json(new { result = false, error = _localizer[updateEventResult.ErrorKey ?? "Input is invalid"].Value });

                return Json(new { result = true, message = _localizer["The calendar event has been successfully updated."].Value });
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

    #region Calendar Shared
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateCalendarShared([FromBody] IEnumerable<CalendarSharedResponse> calendarShareds)
    {
        try
        {
            if (ModelState.IsValid)
            {
                ServiceResult sharedResult = await _calendarService.UpdateCalendarSharedAsync(calendarShareds.Select(c => c.Adapt<CalendarSharedRequest>()));

                if (!sharedResult.Success)
                    return Json(new { result = false, error = _localizer[sharedResult.ErrorKey ?? "Input is invalid"].Value });

                return Json(new { result = true, message = _localizer["The calendar shared has been successfully updated."].Value });
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

    #region Other Calendar
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateOtherCalendar([FromBody] IEnumerable<OtherCalendarResponse> otherCalendars)
    {
        try
        {
            if (ModelState.IsValid)
            {
                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = _localizer["Login required."].Value });

                AccountResponse loginedAccount = _sessionService.GetAccount()!;

                if (loginedAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = _localizer["Login required."].Value });

                ServiceResult otherCalendarResult = await _calendarService.UpdateOtherCalendarAsync(
                    loginedAccount.Email,
                    otherCalendars.Select(c => c.Adapt<OtherCalendarRequest>()));

                if (!otherCalendarResult.Success)
                    return Json(new { result = false, error = _localizer[otherCalendarResult.ErrorKey ?? "Input is invalid"].Value });

                return Json(new { result = true, message = _localizer["The other calendar has been successfully updated."].Value });
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

    #region Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteCalendar([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            CalendarRequest tempCalendar = calendarInputViewModel?.Calendars?.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email;

            ServiceResult deleteCalendarResult = await _calendarService.DeleteCalendarAsync(tempCalendar);

            if (!deleteCalendarResult.Success)
                return Json(new { result = false, error = _localizer[deleteCalendarResult.ErrorKey ?? "Input is invalid"].Value });

            return Json(new { result = true, message = _localizer["The calendar has been successfully deleted."].Value, calendar = new CalendarRequest() { Id = tempCalendar.Id } });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteCalendarEvent(int id)
    {
        try
        {
            string email = _sessionService.GetAccount()!.Email;

            ServiceResult deleteEventResult = await _calendarService.DeleteCalendarEventAsync(id, email);

            if (!deleteEventResult.Success)
                return Json(new { result = false, error = _localizer[deleteEventResult.ErrorKey ?? "Input is invalid"].Value });

            return Json(new { result = true, message = _localizer["The calendar event has been successfully deleted."].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion
}
