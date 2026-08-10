using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Website.Attributes;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Maroik.Website.Mappings;
using Maroik.Website.Models.ViewModels.Calendar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Maroik.Website.Controllers;

/// <summary>
/// Manages calendars and calendar events: CRUD, sharing, subscriptions,
/// Summernote image uploads, and role-differentiated index views.
/// </summary>
public class CalendarController : Controller
{
    private readonly IRsaService _rsa;
    private readonly IHtmlLocalizer<CalendarController> _localizer;
    private readonly ICalendarService _calendarService;
    private readonly ISessionService _sessionService;
    private readonly IOptions<ServerSetting> _serverSettings;

    /// <summary>Initializes a new instance of <see cref="CalendarController"/> with the supplied dependencies.</summary>
    public CalendarController(
        IHtmlLocalizer<CalendarController> localizer,
        ICalendarService calendarService,
        IRsaService rsa,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService)
    {
        _rsa = rsa;
        _sessionService = sessionService;
        _localizer = localizer;
        _calendarService = calendarService;
        _serverSettings = serverSettings;
    }

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
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarRequest tempCalendar = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email!;

            ServiceResult result = await _calendarService.CreateCalendarAsync(tempCalendar, HttpContext.RequestAborted);

            if (!result.Success)
                return Json(new { result = false, error = _localizer[result.ErrorKey].Value });

            // Strip fields the client doesn't need back (and shouldn't see echoed), keeping the
            // JSON payload limited to what the calendar picker UI uses to render the new entry.
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
    /// <summary>Creates a new calendar event (with optional file attachment and reminders).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateCalendarEvent([FromForm] CalendarEventInputViewModel calendarEventInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            calendarEventInputViewModel.Description ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to create calendar event"].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            if (loggedInAccount.Role is not (Role.Admin or Role.User))
                return Json(new { result = false, error = _localizer["Please Login to create calendar event"].Value });

            ServiceResult createEventResult = await _calendarService.CreateCalendarEventAsync(
                calendarEventInputViewModel.ToCalendarEventRequest(),
                loggedInAccount.Email!,
                $"{loggedInAccount.Role}Index",
                calendarEventInputViewModel.SerializedCalendarReminders.ToReminderInfoList(),
                await calendarEventInputViewModel.CalendarEventUploadedFile.ToAttachedFileInfoAsync(HttpContext.RequestAborted),
                HttpContext.RequestAborted);

            return !createEventResult.Success ? Json(new { result = false, error = _localizer[createEventResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar event has been successfully created."].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
        if (summernoteImageFile.Length <= 0)
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length > _serverSettings.Value.MaxAttachedFileSizeBytes)
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than {0}MB.", _serverSettings.Value.MaxAttachedFileSizeBytes / (1024 * 1024)].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await summernoteImageFile.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        var loggedInAccount = _sessionService.GetAccount()!;
        string roleIndex = $"{loggedInAccount.Role}Index";

        var file = new AttachedFileDto
        {
            Bytes = bytes,
            ContentType = summernoteImageFile.ContentType,
            FileName = summernoteImageFile.FileName,
            Size = summernoteImageFile.Length
        };

        SummernoteUploadResult uploadResult = await _calendarService.UploadSummernoteImageAsync(file, roleIndex, HttpContext.RequestAborted);

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = _localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        return Ok(new
        {
            result = true,
            file = File(uploadResult.FileBytes!, uploadResult.ContentType!, uploadResult.FileName!),
            filePath = _rsa.Encrypt(uploadResult.FilePath!)
        });
    }
    #endregion

    #endregion

    #region Read

    /// <summary>Returns the admin calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> AdminIndex()
    {
        AccountResponse loggedInAccount = _sessionService.GetAccount()!;
        string userTz = loggedInAccount.TimeZoneIanaId ?? "UTC";

        List<CalendarResponse> calendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        if (calendars.Count == 0)
        {
            // First visit for this account: auto-provision a default personal calendar
            // so the grid is never empty and events can be created immediately.
            await _calendarService.EnsureDefaultCalendarAsync(new CalendarRequest
            {
                AccountEmail = loggedInAccount.Email!,
                Name = loggedInAccount.Nickname!,
                TimeZoneIanaId = loggedInAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e"
            }, HttpContext.RequestAborted);
            calendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        }

        CalendarOutputViewModel vm = new()
        {
            Calendars = calendars,
            CalendarEventOutputViewModels = await _calendarService.GetCalendarEventViewModelsAsync(calendars, userTz, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTz
        };
        vm.PopulateTimeData(userTz);
        return View(vm);
    }

    /// <summary>Returns the user calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> UserIndex()
    {
        AccountResponse loggedInAccount = _sessionService.GetAccount()!;
        string userTz = loggedInAccount.TimeZoneIanaId ?? "UTC";

        List<CalendarResponse> calendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        if (calendars.Count == 0)
        {
            // Same first-visit provisioning as AdminIndex (see comment there).
            await _calendarService.EnsureDefaultCalendarAsync(new CalendarRequest
            {
                AccountEmail = loggedInAccount.Email!,
                Name = loggedInAccount.Nickname!,
                TimeZoneIanaId = loggedInAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e"
            }, HttpContext.RequestAborted);
            calendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        }

        List<CalendarResponse> allCalendars = await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
        List<OtherCalendarResponse> otherCalendarSubscriptions = await _calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        List<CalendarResponse> otherCalendars =
        [
            .. (from c in allCalendars
                join oc in otherCalendarSubscriptions on c.Id equals oc.CalendarId
                select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
            .OrderBy(x => x.Name)
        ];

        CalendarOutputViewModel vm = new()
        {
            Calendars = calendars,
            CalendarEventOutputViewModels = await _calendarService.GetCalendarEventViewModelsAsync(calendars, userTz, null, HttpContext.RequestAborted),
            OtherCalendars = otherCalendars,
            OtherCalendarEventOutputViewModels = await _calendarService.GetCalendarEventViewModelsAsync(otherCalendars, userTz, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTz
        };
        vm.PopulateTimeData(userTz);
        return View(vm);
    }

    /// <summary>Returns the anonymous calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> AnonymousIndex()
    {
        const string userTz = "UTC";
        AccountResponse loggedInAccount = new() { Role = Role.Anonymous, TimeZoneIanaId = userTz };

        List<CalendarResponse> allCalendars = await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
        List<CalendarSharedResponse> sharedCalendars = await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted);
        List<CalendarResponse> otherCalendars =
        [
            .. (from c in allCalendars
                join cs in sharedCalendars.Where(x => x.Anonymous) on c.Id equals cs.CalendarId
                select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
            .OrderBy(x => x.Name)
        ];

        CalendarOutputViewModel vm = new()
        {
            OtherCalendars = otherCalendars,
            OtherCalendarEventOutputViewModels = await _calendarService.GetCalendarEventViewModelsAsync(otherCalendars, userTz, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTz
        };
        vm.PopulateTimeData(userTz);
        return View(vm);
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
            var tempCalendars =
                await _calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email!,
                    HttpContext.RequestAborted);

            if (tempCalendars.Count == 0)
            {
                return Json(new { result = false, error = _localizer["No calendar exists"].Value });
            }

            var tempCalendar = tempCalendars.FirstOrDefault(a => a.Id == id);

            return tempCalendar == null
                ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                : (IActionResult)Json(new { result = true, calendar = tempCalendar });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            List<CalendarResponse> calendars = await _calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email!, HttpContext.RequestAborted);

            return Json(new { result = true, calendars });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (isAccountSessionExist)
            {
                List<CalendarResponse> calendars = await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
                List<OtherCalendarResponse> otherCalendars = await _calendarService.GetOtherCalendarsAsync(_sessionService.GetAccount()!.Email!, HttpContext.RequestAborted);

                List<CalendarResponse> tempOtherCalendars =
                [
                    .. (from calendar in calendars
                        join otherCalendar in otherCalendars
                            on calendar.Id equals otherCalendar.CalendarId
                        select new CalendarResponse()
                        {
                            Id = calendar.Id, Name = calendar.Name, HtmlColorCode = calendar.HtmlColorCode,
                        }).OrderBy(x => x.Name)
                ];

                return Json(new { result = true, tempOtherCalendars });
            }
            else
            {
                List<CalendarResponse> tempOtherCalendars = [];
                var temporaryOtherCalendars = (from calendar in await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted)
                                               join calendarShared in (await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted)).Where(x => x.Anonymous).ToList()
                                               on calendar.Id equals calendarShared.CalendarId
                                               select new
                                               {
                                                   calendar.Id,
                                                   calendar.Name,
                                                   calendar.HtmlColorCode
                                               }).OrderBy(x => x.Name).ToList();

                tempOtherCalendars.AddRange(temporaryOtherCalendars.Select(tempOtherCalendar => new CalendarResponse { Id = tempOtherCalendar.Id, Name = tempOtherCalendar.Name, HtmlColorCode = tempOtherCalendar.HtmlColorCode }));

                return Json(new { result = true, tempOtherCalendars });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Checks whether a calendar event with the given ID exists and returns its full detail.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsCalendarEventExists(int id)
    {
        try
        {
            AccountResponse loggedInAccount = _sessionService.GetAccount()!;
            List<CalendarResponse> calendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);

            CalendarEventResponse? tempCalendarEvent = await _calendarService.GetCalendarEventAsync(id, HttpContext.RequestAborted);

            if (tempCalendarEvent is not { Id: > 0 })
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarResponse tempCalendar = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId) ?? new CalendarResponse();
            if (!(tempCalendar.Id > 0))
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarEventOutputViewModel calendarEvent = tempCalendarEvent.ToDisplayViewModel(
                loggedInAccount.TimeZoneIanaId ?? "UTC", null, forGrid: false);

            (calendarEvent.Description, _) = await _calendarService.PrepareHtmlForDisplayAsync(tempCalendarEvent.Description ?? "", HttpContext.RequestAborted);
            calendarEvent.Location = tempCalendarEvent.Location;
            calendarEvent.Status = tempCalendarEvent.Status;
            calendarEvent.CalendarEventAttachedFile = await _calendarService.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id, HttpContext.RequestAborted);

            if (!string.IsNullOrEmpty(calendarEvent.CalendarEventAttachedFile?.Path))
            {
                byte[]? fileData = await _calendarService.DownloadFileAsync(calendarEvent.CalendarEventAttachedFile.Path, HttpContext.RequestAborted);
                if (fileData != null)
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (!provider.TryGetContentType(calendarEvent.CalendarEventAttachedFile.Path, out string? contentType))
                        contentType = "application/octet-stream";
                    calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                    calendarEvent.CalendarEventAttachedFileContentType = contentType;
                }
            }

            calendarEvent.Calendars =
                [.. calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name)];
            calendarEvent.SerializedCalendarReminders = (await _calendarService.GetCalendarEventRemindersAsync(tempCalendarEvent.Id, HttpContext.RequestAborted)).ToSerializedReminders();
            calendarEvent.HtmlColorCode = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId)?.HtmlColorCode;

            return Json(new { result = true, calendarEvent });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Checks whether an event in a shared/other calendar exists and returns its full detail.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> IsOtherCalendarEventExists(int id)
    {
        try
        {
            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            AccountResponse loggedInAccount;
            List<CalendarResponse> calendars;

            if (isAccountSessionExist)
            {
                loggedInAccount = _sessionService.GetAccount()!;
                calendars =
                [
                    .. from c in await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted)
                    join oc in await _calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!,
                            HttpContext.RequestAborted)
                        on c.Id equals oc.CalendarId
                    select c
                ];
            }
            else
            {
                loggedInAccount = new AccountResponse { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
                calendars =
                [
                    .. (from c in await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted)
                        join cs in (await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted))
                            .Where(x => x.Anonymous).ToList()
                            on c.Id equals cs.CalendarId
                        select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
                    .OrderBy(x => x.Name)
                ];
            }

            CalendarEventResponse? tempCalendarEvent = await _calendarService.GetCalendarEventAsync(id, HttpContext.RequestAborted);

            if (tempCalendarEvent is not { Id: > 0 })
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarResponse tempCalendar = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId) ?? new CalendarResponse();
            if (!(tempCalendar.Id > 0))
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            CalendarEventOutputViewModel calendarEvent = tempCalendarEvent.ToDisplayViewModel(
                loggedInAccount.TimeZoneIanaId ?? "UTC", null, forGrid: false);

            (calendarEvent.Description, _) = await _calendarService.PrepareHtmlForDisplayAsync(tempCalendarEvent.Description ?? "", HttpContext.RequestAborted);
            calendarEvent.Location = tempCalendarEvent.Location;
            calendarEvent.Status = tempCalendarEvent.Status;
            calendarEvent.CalendarEventAttachedFile = await _calendarService.GetCalendarEventAttachedFileAsync(tempCalendarEvent.Id, HttpContext.RequestAborted);

            if (!string.IsNullOrEmpty(calendarEvent.CalendarEventAttachedFile?.Path))
            {
                byte[]? fileData = await _calendarService.DownloadFileAsync(calendarEvent.CalendarEventAttachedFile.Path, HttpContext.RequestAborted);
                if (fileData != null)
                {
                    FileExtensionContentTypeProvider provider = new();
                    if (!provider.TryGetContentType(calendarEvent.CalendarEventAttachedFile.Path, out string? contentType))
                        contentType = "application/octet-stream";
                    calendarEvent.CalendarEventAttachedFileBase64Data = Convert.ToBase64String(fileData);
                    calendarEvent.CalendarEventAttachedFileContentType = contentType;
                }
            }

            calendarEvent.Calendars =
                [.. calendars.Select(x => new CalendarResponse { Id = x.Id, Name = x.Name }).OrderBy(x => x.Name)];
            calendarEvent.SerializedCalendarReminders = (await _calendarService.GetCalendarEventRemindersAsync(tempCalendarEvent.Id, HttpContext.RequestAborted)).ToSerializedReminders();
            calendarEvent.HtmlColorCode = calendars.FirstOrDefault(x => x.Id == tempCalendarEvent.CalendarId)?.HtmlColorCode;

            return Json(new { result = true, calendarEvent });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns calendar events for the requested calendars as JSON.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    [RequiredHttpPostAccess(Role = Role.Anonymous)]
    public async Task<IActionResult> GetCalendarEvents([FromBody] CalendarInputViewModel calendarInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            AccountResponse loggedInAccount;
            List<CalendarResponse> ownedCalendars;
            List<CalendarResponse> otherCalendars;

            if (isAccountSessionExist)
            {
                loggedInAccount = _sessionService.GetAccount()!;
                ownedCalendars = await _calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);

                otherCalendars =
                [
                    .. (from c in await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted)
                        join oc in await _calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!,
                                HttpContext.RequestAborted)
                            on c.Id equals oc.CalendarId
                        select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
                    .OrderBy(x => x.Name)
                ];
            }
            else
            {
                loggedInAccount = new AccountResponse { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
                ownedCalendars = [];
                otherCalendars =
                [
                    .. (from c in await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted)
                        join cs in (await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted))
                            .Where(x => x.Anonymous).ToList()
                            on c.Id equals cs.CalendarId
                        select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
                    .OrderBy(x => x.Name)
                ];
            }

            string userTz = loggedInAccount.TimeZoneIanaId ?? "UTC";
            List<CalendarEventOutputViewModel> calendarEvents = [];

            // Ignore any requested calendar the account doesn't own and isn't subscribed to,
            // so a tampered request body can't be used to read another user's private events.
            List<long> authorizedCalendarIds =
            [
                .. (calendarInputViewModel.Calendars)
                .Select(c => c.Id)
                .Where(id => ownedCalendars.Any(x => x.Id == id) || otherCalendars.Any(x => x.Id == id))
            ];

            // Fetch every authorized calendar's events in a single query instead of one round trip per calendar.
            ILookup<long, CalendarEventResponse> eventsByCalendarId =
                await _calendarService.GetCalendarEventsAsync(authorizedCalendarIds, HttpContext.RequestAborted);

            calendarEvents.AddRange(from inputCalendar in calendarInputViewModel.Calendars
                let isOwned = ownedCalendars.Any(x => x.Id == inputCalendar.Id)
                let isOther = !isOwned && otherCalendars.Any(x => x.Id == inputCalendar.Id)
                where isOwned || isOther
                let calendarType = isOwned
                    ? EnumHelper.GetDescription(CalendarType.My)
                    : EnumHelper.GetDescription(CalendarType.Other)
                let htmlColorCode = isOwned
                    ? ownedCalendars.FirstOrDefault(x => x.Id == inputCalendar.Id)?.HtmlColorCode
                    : otherCalendars.FirstOrDefault(x => x.Id == inputCalendar.Id)?.HtmlColorCode
                from evt in eventsByCalendarId[inputCalendar.Id]
                select evt.ToDisplayViewModel(userTz, htmlColorCode, calendarType));

            return Json(new { result = true, calendarEvents = JsonConvert.SerializeObject(calendarEvents) });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            List<CalendarResponse> calendars =
            [
                .. await _calendarService.GetCalendarsAsync(_sessionService.GetAccount()!.Email!,
                    HttpContext.RequestAborted)
            ];

            // A CalendarShared row may not exist yet for calendars created before sharing was
            // configured, so backfill any missing ones (single query) before reading the list below.
            await _calendarService.EnsureCalendarSharedAsync(calendars.Select(c => c.Id), HttpContext.RequestAborted);

            List<CalendarSharedResponse> tempCalendarShareds =
            [
                .. from calendarShared in
                    (await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted)).ToList()
                let item = calendars.FirstOrDefault(x => x.Id == calendarShared.CalendarId) ??
                           new CalendarResponse()
                where item.Id > 0
                select calendarShared
            ];

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

    /// <summary>Returns the list of shared/public calendars the logged-in user may browse.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetBrowseCalendarsOfInterest()
    {
        try
        {
            AccountResponse loggedInAccount = _sessionService.GetAccount()!;
            List<CalendarResponse> allCalendars = await _calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
            List<CalendarSharedResponse> calendarShareds =
            [
                .. (await _calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted)).Where(x => x.User)
            ];
            List<OtherCalendarResponse> otherCalendars = await _calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);

            var browseCalendarsOfInterests = (from calendar in allCalendars
                                              join calendarShared in calendarShareds
                                              on calendar.Id equals calendarShared.CalendarId
                                              join otherCalendar in otherCalendars
                                              on calendar.Id equals otherCalendar.CalendarId into otherCalendarGroup
                                              from otherCalendar in otherCalendarGroup.DefaultIfEmpty()
                                              select new
                                              {
                                                  calendar.Id,
                                                  calendar.Name,
                                                  Checked = otherCalendar != null
                                              }).OrderBy(x => x.Name).ToList();

            return Json(new { result = true, browseCalendarsOfInterests });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            if (!ModelState.IsValid)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }

            try
            {
                CalendarRequest calendarRequest = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
                calendarRequest.AccountEmail = _sessionService.GetAccount()!.Email;

                ServiceResult updateResult = await _calendarService.UpdateCalendarAsync(calendarRequest, HttpContext.RequestAborted);

                return !updateResult.Success ? Json(new { result = false, error = _localizer[updateResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar has been successfully updated."].Value, calendar = new CalendarRequest { Id = calendarRequest.Id, Name = calendarRequest.Name, HtmlColorCode = calendarRequest.HtmlColorCode } });
            }
            catch
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
    /// <summary>Updates an existing calendar event (with optional file attachment and reminder changes).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateCalendarEvent([FromForm] CalendarEventInputViewModel calendarEventInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            calendarEventInputViewModel.Description ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to update calendar event"].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            if (loggedInAccount.Role is not (Role.Admin or Role.User))
                return Json(new { result = false, error = _localizer["Please Login to update calendar event"].Value });

            ServiceResult updateEventResult = await _calendarService.UpdateCalendarEventAsync(
                calendarEventInputViewModel.ToCalendarEventRequest(),
                loggedInAccount.Email!,
                $"{loggedInAccount.Role}Index",
                calendarEventInputViewModel.SerializedCalendarReminders.ToReminderInfoList(),
                await calendarEventInputViewModel.CalendarEventUploadedFile.ToAttachedFileInfoAsync(HttpContext.RequestAborted),
                HttpContext.RequestAborted);

            return !updateEventResult.Success ? Json(new { result = false, error = _localizer[updateEventResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar event has been successfully updated."].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #region Calendar Shared
    /// <summary>Updates an existing CalendarShared record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateCalendarShared([FromBody] IEnumerable<CalendarSharedRequest> calendarShareds)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }

            ServiceResult sharedResult = await _calendarService.UpdateCalendarSharedAsync(calendarShareds, HttpContext.RequestAborted);

            return !sharedResult.Success ? Json(new { result = false, error = _localizer[sharedResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar shared has been successfully updated."].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            if (ModelState.IsValid)
            {
                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = _localizer["Login required."].Value });

                AccountResponse loggedInAccount = _sessionService.GetAccount()!;

                if (loggedInAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = _localizer["Login required."].Value });

                ServiceResult otherCalendarResult = await _calendarService.UpdateOtherCalendarAsync(
                    loggedInAccount.Email!,
                    otherCalendars.Select(c => new OtherCalendarRequest { AccountEmail = c.AccountEmail, CalendarId = c.CalendarId }),
                    HttpContext.RequestAborted);

                return !otherCalendarResult.Success ? Json(new { result = false, error = _localizer[otherCalendarResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The other calendar has been successfully updated."].Value });
            }

            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            CalendarRequest tempCalendar = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
            tempCalendar.AccountEmail = _sessionService.GetAccount()!.Email!;

            ServiceResult deleteCalendarResult = await _calendarService.DeleteCalendarAsync(tempCalendar, HttpContext.RequestAborted);

            return !deleteCalendarResult.Success ? Json(new { result = false, error = _localizer[deleteCalendarResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar has been successfully deleted."].Value, calendar = new CalendarRequest { Id = tempCalendar.Id } });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            string email = _sessionService.GetAccount()!.Email!;

            ServiceResult deleteEventResult = await _calendarService.DeleteCalendarEventAsync(id, email, HttpContext.RequestAborted);

            return !deleteEventResult.Success ? Json(new { result = false, error = _localizer[deleteEventResult.ErrorKey].Value }) : Json(new { result = true, message = _localizer["The calendar event has been successfully deleted."].Value });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

}
