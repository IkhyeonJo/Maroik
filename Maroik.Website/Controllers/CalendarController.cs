using Maroik.Core.Domain.Media;
using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Calendar;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Website.Attributes;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Maroik.Website.Mappings;
using Maroik.Website.Models.ViewModels.Calendar;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Controllers;

/// <summary>
/// Manages calendars and calendar events: CRUD, sharing, subscriptions,
/// Summernote image uploads, and role-differentiated index views.
/// </summary>
public class CalendarController(
    IHtmlLocalizer<CalendarController> localizer,
    ILogger<CalendarController> logger,
    ICalendarService calendarService,
    IRsaService rsa,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService) : Controller
{
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

            CalendarRequest tempCalendar = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
            string accountEmail = sessionService.GetAccount()!.Email!;

            ServiceResult result = await calendarService.CreateCalendarAsync(accountEmail, tempCalendar, HttpContext.RequestAborted);

            if (!result.Success)
                return Json(new { result = false, error = localizer[result.ErrorKey, result.ErrorArgs].ToPlainString() });

            // Strip fields the client doesn't need back (and shouldn't see echoed), keeping the
            // JSON payload limited to what the calendar picker UI uses to render the new entry.
            tempCalendar.AccountEmail = null;
            tempCalendar.Description = null;
            tempCalendar.TimeZoneIanaId = null;
            return Json(new { result = true, message = localizer["The calendar has been successfully created."].Value, calendar = tempCalendar });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create calendar for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            calendarEventInputViewModel.Description ??= "";

            bool isAccountSessionExist = sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Please Login to create calendar event"].Value });

            AccountResponse loggedInAccount = sessionService.GetAccount()!;

            if (loggedInAccount.Role is not (Role.Admin or Role.User))
                return Json(new { result = false, error = localizer["Please Login to create calendar event"].Value });

            ServiceResult createEventResult = await calendarService.CreateCalendarEventAsync(
                calendarEventInputViewModel.ToCalendarEventRequest(),
                loggedInAccount.Email!,
                $"{loggedInAccount.Role}Index",
                calendarEventInputViewModel.SerializedCalendarReminders.ToReminderInfoList(),
                await calendarEventInputViewModel.CalendarEventUploadedFile.ToAttachedFileInfoAsync(serverSettings.Value.MaxAttachedFileSizeBytes, HttpContext.RequestAborted),
                HttpContext.RequestAborted);

            return !createEventResult.Success ? Json(new { result = false, error = localizer[createEventResult.ErrorKey, createEventResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar event has been successfully created."].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create calendar event for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
    public async Task<IActionResult> UploadImageFile(IFormFile? summernoteImageFile)
    {
        if (summernoteImageFile == null)
            return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > serverSettings.Value.MaxAttachedFileSizeBytes)
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than {0}MB.", serverSettings.Value.MaxAttachedFileSizeBytes / (1024 * 1024)].ToPlainString() });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (!ImageUploadPolicy.IsAllowedExtension(ext))
            return Ok(new { result = false, errorMessage = localizer["Only .jpg or jpeg or .png file allowed."].Value });

        var loggedInAccount = sessionService.GetAccount()!;
        string roleIndex = $"{loggedInAccount.Role}Index";

        AttachedFileDto file = (await summernoteImageFile.ToAttachedFileInfoAsync(serverSettings.Value.MaxAttachedFileSizeBytes, HttpContext.RequestAborted))!;

        SummernoteUploadResult uploadResult = await calendarService.UploadSummernoteImageAsync(file, roleIndex, HttpContext.RequestAborted);

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        // The Summernote editor only needs the raw bytes (base64) + content type to build an object
        // URL for the inserted <img>; return those explicitly rather than serializing a whole
        // FileContentResult and relying on its property names (matches Forum / Management).
        return Ok(new
        {
            result = true,
            file = new
            {
                fileContents = Convert.ToBase64String(uploadResult.FileBytes!),
                contentType = uploadResult.ContentType!
            },
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
        AccountResponse loggedInAccount = sessionService.GetAccount()!;
        string userTimezone = loggedInAccount.TimeZoneIanaId ?? "UTC";

        List<CalendarResponse> calendars = await calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        if (calendars.Count == 0)
        {
            // First visit for this account: auto-provision a default personal calendar
            // so the grid is never empty and events can be created immediately.
            await calendarService.EnsureDefaultCalendarAsync(loggedInAccount.Email!, new CalendarRequest
            {
                Name = loggedInAccount.Nickname!,
                TimeZoneIanaId = loggedInAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e"
            }, HttpContext.RequestAborted);
            calendars = await calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        }

        CalendarOutputViewModel vm = new()
        {
            Calendars = calendars,
            CalendarEventOutputViewModels = await calendarService.GetCalendarEventViewModelsAsync(calendars, userTimezone, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTimezone
        };
        vm.PopulateTimeData(userTimezone);
        return View(vm);
    }

    /// <summary>Returns the user calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> UserIndex()
    {
        AccountResponse loggedInAccount = sessionService.GetAccount()!;
        string userTimezone = loggedInAccount.TimeZoneIanaId ?? "UTC";

        List<CalendarResponse> calendars = await calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        if (calendars.Count == 0)
        {
            // Same first-visit provisioning as AdminIndex (see comment there).
            await calendarService.EnsureDefaultCalendarAsync(loggedInAccount.Email!, new CalendarRequest
            {
                Name = loggedInAccount.Nickname!,
                TimeZoneIanaId = loggedInAccount.TimeZoneIanaId,
                HtmlColorCode = "#fc330e"
            }, HttpContext.RequestAborted);
            calendars = await calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
        }

        List<CalendarResponse> allCalendars = await calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
        List<OtherCalendarResponse> otherCalendarSubscriptions = await calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);
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
            CalendarEventOutputViewModels = await calendarService.GetCalendarEventViewModelsAsync(calendars, userTimezone, null, HttpContext.RequestAborted),
            OtherCalendars = otherCalendars,
            OtherCalendarEventOutputViewModels = await calendarService.GetCalendarEventViewModelsAsync(otherCalendars, userTimezone, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTimezone
        };
        vm.PopulateTimeData(userTimezone);
        return View(vm);
    }

    /// <summary>Returns the anonymous calendar view.</summary>
    [HttpGet]
    public async Task<IActionResult> AnonymousIndex()
    {
        const string userTimezone = "UTC";
        AccountResponse loggedInAccount = new() { Role = Role.Anonymous, TimeZoneIanaId = userTimezone };

        List<CalendarResponse> allCalendars = await calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
        List<CalendarSharedResponse> sharedCalendars = await calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted);
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
            OtherCalendarEventOutputViewModels = await calendarService.GetCalendarEventViewModelsAsync(otherCalendars, userTimezone, null, HttpContext.RequestAborted),
            LoggedInAccount = loggedInAccount,
            LoggedInAccountTimeZoneIanaId = userTimezone
        };
        vm.PopulateTimeData(userTimezone);
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
                await calendarService.GetCalendarsAsync(sessionService.GetAccount()!.Email!,
                    HttpContext.RequestAborted);

            if (tempCalendars.Count == 0)
            {
                return Json(new { result = false, error = localizer["No calendar exists"].Value });
            }

            var tempCalendar = tempCalendars.FirstOrDefault(a => a.Id == id);

            return tempCalendar == null
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : (IActionResult)Json(new { result = true, calendar = tempCalendar });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check calendar existence for id {CalendarId}", id);
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
            List<CalendarResponse> calendars = await calendarService.GetCalendarsAsync(sessionService.GetAccount()!.Email!, HttpContext.RequestAborted);

            return Json(new { result = true, calendars });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get calendars for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            List<CalendarResponse> tempOtherCalendars =
                await calendarService.GetVisibleOtherCalendarsAsync(sessionService.GetAccount(), HttpContext.RequestAborted);

            return Json(new { result = true, tempOtherCalendars });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get other calendars for account {AccountEmail}", sessionService.GetAccount()?.Email);
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            AccountResponse loggedInAccount = sessionService.GetAccount()!;
            List<CalendarResponse> calendars = await calendarService.GetCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);

            CalendarEventOutputViewModel? calendarEvent = await calendarService.GetCalendarEventDetailViewModelAsync(
                calendars, loggedInAccount.TimeZoneIanaId ?? "UTC", id, HttpContext.RequestAborted);

            return calendarEvent == null
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : Json(new { result = true, calendarEvent });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check calendar event existence for id {CalendarEventId}", id);
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            AccountResponse? sessionAccount = sessionService.GetAccount();
            AccountResponse loggedInAccount = sessionAccount ?? new AccountResponse { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
            List<CalendarResponse> calendars =
                await calendarService.GetVisibleOtherCalendarsAsync(sessionAccount, HttpContext.RequestAborted);

            CalendarEventOutputViewModel? calendarEvent = await calendarService.GetCalendarEventDetailViewModelAsync(
                calendars, loggedInAccount.TimeZoneIanaId ?? "UTC", id, HttpContext.RequestAborted);

            return calendarEvent == null
                ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                : Json(new { result = true, calendarEvent });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to check other calendar event existence for id {CalendarEventId}", id);
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            AccountResponse? sessionAccount = sessionService.GetAccount();
            AccountResponse loggedInAccount = sessionAccount ?? new AccountResponse { Role = Role.Anonymous, TimeZoneIanaId = "UTC" };
            List<CalendarResponse> ownedCalendars = sessionAccount != null
                ? await calendarService.GetCalendarsAsync(sessionAccount.Email!, HttpContext.RequestAborted)
                : [];
            List<CalendarResponse> otherCalendars =
                await calendarService.GetVisibleOtherCalendarsAsync(sessionAccount, HttpContext.RequestAborted);

            string userTimezone = loggedInAccount.TimeZoneIanaId ?? "UTC";
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
            IReadOnlyDictionary<long, List<CalendarEventResponse>> eventsByCalendarId =
                await calendarService.GetCalendarEventsAsync(authorizedCalendarIds, HttpContext.RequestAborted);

            calendarEvents.AddRange(from inputCalendar in calendarInputViewModel.Calendars
                let isOwned = ownedCalendars.Any(x => x.Id == inputCalendar.Id)
                let isOther = !isOwned && otherCalendars.Any(x => x.Id == inputCalendar.Id)
                where isOwned || isOther
                let calendarType = isOwned
                    ? CalendarTypes.My
                    : CalendarTypes.Other
                let htmlColorCode = isOwned
                    ? ownedCalendars.FirstOrDefault(x => x.Id == inputCalendar.Id)?.HtmlColorCode
                    : otherCalendars.FirstOrDefault(x => x.Id == inputCalendar.Id)?.HtmlColorCode
                from evt in eventsByCalendarId.GetValueOrDefault(inputCalendar.Id, [])
                select evt.ToDisplayViewModel(userTimezone, htmlColorCode, calendarType));

            // Matches main: an empty result set for the requested range/calendars is reported the
            // same way as any other "nothing to return" case, not as a distinct success payload.
            return calendarEvents.Count > 0
                ? Json(new { result = true, calendarEvents = JsonSerialization.ToClientJson(calendarEvents) })
                : Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get calendar events for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            List<CalendarResponse> calendars =
            [
                .. await calendarService.GetCalendarsAsync(sessionService.GetAccount()!.Email!,
                    HttpContext.RequestAborted)
            ];

            // A CalendarShared row may not exist yet for calendars created before sharing was
            // configured, so backfill any missing ones (single query) before reading the list below.
            await calendarService.EnsureCalendarSharedAsync(calendars.Select(c => c.Id), HttpContext.RequestAborted);

            List<CalendarSharedResponse> tempCalendarShareds =
            [
                .. from calendarShared in
                    (await calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted)).ToList()
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get calendar shareds for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            AccountResponse loggedInAccount = sessionService.GetAccount()!;
            List<CalendarResponse> allCalendars = await calendarService.GetAllCalendarsAsync(HttpContext.RequestAborted);
            List<CalendarSharedResponse> calendarShareds =
            [
                .. (await calendarService.GetCalendarSharedAsync(HttpContext.RequestAborted)).Where(x => x.User)
            ];
            List<OtherCalendarResponse> otherCalendars = await calendarService.GetOtherCalendarsAsync(loggedInAccount.Email!, HttpContext.RequestAborted);

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get browse calendars of interest for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            if (!ModelState.IsValid)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            CalendarRequest calendarRequest = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
            string accountEmail = sessionService.GetAccount()!.Email!;

            ServiceResult updateResult = await calendarService.UpdateCalendarAsync(accountEmail, calendarRequest, HttpContext.RequestAborted);

            return !updateResult.Success ? Json(new { result = false, error = localizer[updateResult.ErrorKey, updateResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar has been successfully updated."].Value, calendar = new CalendarRequest { Id = calendarRequest.Id, Name = calendarRequest.Name, HtmlColorCode = calendarRequest.HtmlColorCode } });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update calendar for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });

            calendarEventInputViewModel.Description ??= "";

            bool isAccountSessionExist = sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = localizer["Please Login to update calendar event"].Value });

            AccountResponse loggedInAccount = sessionService.GetAccount()!;

            if (loggedInAccount.Role is not (Role.Admin or Role.User))
                return Json(new { result = false, error = localizer["Please Login to update calendar event"].Value });

            ServiceResult updateEventResult = await calendarService.UpdateCalendarEventAsync(
                calendarEventInputViewModel.ToCalendarEventRequest(),
                loggedInAccount.Email!,
                $"{loggedInAccount.Role}Index",
                calendarEventInputViewModel.SerializedCalendarReminders.ToReminderInfoList(),
                await calendarEventInputViewModel.CalendarEventUploadedFile.ToAttachedFileInfoAsync(serverSettings.Value.MaxAttachedFileSizeBytes, HttpContext.RequestAborted),
                HttpContext.RequestAborted);

            return !updateEventResult.Success ? Json(new { result = false, error = localizer[updateEventResult.ErrorKey, updateEventResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar event has been successfully updated."].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update calendar event for account {AccountEmail}", sessionService.GetAccount()?.Email);
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            ServiceResult sharedResult = await calendarService.UpdateCalendarSharedAsync(sessionService.GetAccount()!.Email!, calendarShareds, HttpContext.RequestAborted);

            return !sharedResult.Success ? Json(new { result = false, error = localizer[sharedResult.ErrorKey, sharedResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar shared has been successfully updated."].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update calendar shared records for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            if (ModelState.IsValid)
            {
                bool isAccountSessionExist = sessionService.GetAccount() != null;
                if (!isAccountSessionExist)
                    return Json(new { result = false, error = localizer["Login required."].Value });

                AccountResponse loggedInAccount = sessionService.GetAccount()!;

                if (loggedInAccount.Role is not (Role.Admin or Role.User))
                    return Json(new { result = false, error = localizer["Login required."].Value });

                ServiceResult otherCalendarResult = await calendarService.UpdateOtherCalendarAsync(
                    loggedInAccount.Email!,
                    otherCalendars.Select(c => new OtherCalendarRequest { CalendarId = c.CalendarId }),
                    HttpContext.RequestAborted);

                return !otherCalendarResult.Success ? Json(new { result = false, error = localizer[otherCalendarResult.ErrorKey, otherCalendarResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The other calendar has been successfully updated."].Value });
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update other calendar for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            CalendarRequest tempCalendar = calendarInputViewModel.Calendars.FirstOrDefault() ?? new CalendarRequest();
            string accountEmail = sessionService.GetAccount()!.Email!;

            ServiceResult deleteCalendarResult = await calendarService.DeleteCalendarAsync(accountEmail, tempCalendar, HttpContext.RequestAborted);

            return !deleteCalendarResult.Success ? Json(new { result = false, error = localizer[deleteCalendarResult.ErrorKey, deleteCalendarResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar has been successfully deleted."].Value, calendar = new CalendarRequest { Id = tempCalendar.Id } });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete calendar for account {AccountEmail}", sessionService.GetAccount()?.Email);
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
            string email = sessionService.GetAccount()!.Email!;

            ServiceResult deleteEventResult = await calendarService.DeleteCalendarEventAsync(id, email, HttpContext.RequestAborted);

            return !deleteEventResult.Success ? Json(new { result = false, error = localizer[deleteEventResult.ErrorKey, deleteEventResult.ErrorArgs].ToPlainString() }) : Json(new { result = true, message = localizer["The calendar event has been successfully deleted."].Value });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete calendar event {CalendarEventId}", id);
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion
}
