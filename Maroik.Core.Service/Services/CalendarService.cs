using Maroik.Core.Domain.Calendar;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="ICalendarService"/> covering the full calendar feature set:
/// calendars, events, reminders, file attachments, sharing, and subscriptions.
/// Event attachments are virus-scanned by ClamAV and image-validated before upload.
/// Files are stored in and retrieved from the file-storage microservice.
/// All multistep write operations use <see cref="IUnitOfWork"/> transactions.
/// </summary>
public class CalendarService : ICalendarService
{
    private readonly ICalendarRepository _calendarRepository;
    private readonly ICalendarEventRepository _calendarEventRepository;
    private readonly ICalendarEventAttachedFileRepository _calendarEventAttachedFileRepository;
    private readonly ICalendarEventReminderRepository _calendarEventReminderRepository;
    private readonly ICalendarSharedRepository _calendarSharedRepository;
    private readonly IOtherCalendarRepository _otherCalendarRepository;
    private readonly IFileClient _fileClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAttachmentContentService _attachmentContent;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<CalendarService> _logger;

    /// <summary>Initializes a new instance of <see cref="CalendarService"/> with the supplied dependencies.</summary>
    public CalendarService(
        ICalendarRepository calendarRepository,
        ICalendarEventRepository calendarEventRepository,
        ICalendarEventAttachedFileRepository calendarEventAttachedFileRepository,
        ICalendarEventReminderRepository calendarEventReminderRepository,
        ICalendarSharedRepository calendarSharedRepository,
        IOtherCalendarRepository otherCalendarRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IAttachmentContentService attachmentContent,
        IOptions<ServerSetting> settings,
        ILogger<CalendarService> logger)
    {
        _calendarRepository = calendarRepository;
        _calendarEventRepository = calendarEventRepository;
        _calendarEventAttachedFileRepository = calendarEventAttachedFileRepository;
        _calendarEventReminderRepository = calendarEventReminderRepository;
        _calendarSharedRepository = calendarSharedRepository;
        _otherCalendarRepository = otherCalendarRepository;
        _fileClient = fileClient;
        _unitOfWork = unitOfWork;
        _attachmentContent = attachmentContent;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetCalendarsAsync(string email, CancellationToken ct = default)
        =>
        [
            .. (await _calendarRepository.GetByAccountEmailAsync(email, ct)).OrderBy(c => c.Name)
            .Select(CalendarMapper.ToResponse)
        ];

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetAllCalendarsAsync(CancellationToken ct = default)
        => [.. (await _calendarRepository.GetAllOrderedByNameAsync(ct)).Select(CalendarMapper.ToResponse)];

    /// <inheritdoc />
    public async Task<ILookup<long, CalendarEventResponse>> GetCalendarEventsAsync(IEnumerable<long> calendarIds, CancellationToken ct = default)
        => (await _calendarEventRepository.GetByCalendarIdsAsync(calendarIds, ct))
            .Select(CalendarEventMapper.ToResponse)
            .ToLookup(e => e.CalendarId);

    /// <inheritdoc />
    public async Task<CalendarEventResponse?> GetCalendarEventAsync(long id, CancellationToken ct = default)
    {
        CalendarEvent? evt = await _calendarEventRepository.FindByIdAsync(id, ct);
        return evt == null ? null : CalendarEventMapper.ToResponse(evt);
    }

    /// <inheritdoc />
    public async Task<CalendarEventAttachedFileDto?> GetCalendarEventAttachedFileAsync(long calendarEventId, CancellationToken ct = default)
    {
        CalendarEventAttachedFile? file = await _calendarEventAttachedFileRepository.FindByCalendarEventIdAsync(calendarEventId, ct);
        return file == null ? null : CalendarEventAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminderDto>> GetCalendarEventRemindersAsync(long calendarEventId, CancellationToken ct = default)
        => (await _calendarEventReminderRepository.GetByCalendarEventIdAsync(calendarEventId, ct)).Select(CalendarEventReminderMapper.ToResponse);

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email, CancellationToken ct = default)
    {
        List<OtherCalendarResponse> subscriptions =
            [.. (await _otherCalendarRepository.GetByAccountEmailAsync(email, ct)).Select(OtherCalendarMapper.ToResponse)];

        // Re-check current sharing status at read time (not just at unsubscribe/unshare time) so a
        // calendar that was turned private stops showing up here even if the subscription-cleanup
        // in UpdateCalendarSharedAsync failed to remove the stale OtherCalendar row.
        HashSet<long> currentlySharedCalendarIds =
            [.. (await _calendarSharedRepository.GetAllAsync(ct)).Where(s => s.User).Select(s => s.Id)];

        return [.. subscriptions.Where(s => currentlySharedCalendarIds.Contains(s.CalendarId))];
    }

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync(CancellationToken ct = default)
        => [.. (await _calendarSharedRepository.GetAllAsync(ct)).Select(CalendarSharedMapper.ToResponse)];

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetVisibleOtherCalendarsAsync(AccountResponse? loggedInAccount, CancellationToken ct = default)
    {
        List<CalendarResponse> allCalendars = await GetAllCalendarsAsync(ct);

        if (loggedInAccount != null)
        {
            List<OtherCalendarResponse> subscriptions = await GetOtherCalendarsAsync(loggedInAccount.Email!, ct);
            return
            [
                .. (from c in allCalendars
                    join oc in subscriptions on c.Id equals oc.CalendarId
                    select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
                .OrderBy(x => x.Name)
            ];
        }

        List<CalendarSharedResponse> anonymousShares = [.. (await GetCalendarSharedAsync(ct)).Where(x => x.Anonymous)];
        return
        [
            .. (from c in allCalendars
                join cs in anonymousShares on c.Id equals cs.CalendarId
                select new CalendarResponse { Id = c.Id, Name = c.Name, HtmlColorCode = c.HtmlColorCode })
            .OrderBy(x => x.Name)
        ];
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureCalendarSharedAsync(IEnumerable<long> calendarIds, CancellationToken ct = default)
    {
        List<long> ids = [.. calendarIds];
        await _unitOfWork.BeginAsync(ct);
        try
        {
            // Single fetch of the whole table, reused for every ID instead of one full scan per calendar.
            // The read + the inserts run in one transaction so two concurrent callers can't both pass
            // the "not present" check for the same id and then race on the CalendarShared primary key.
            List<CalendarShared> shares = await _calendarSharedRepository.GetAllAsync(ct);
            HashSet<long> existingIds = [.. shares.Select(s => s.Id)];

            foreach (long id in ids.Where(id => !existingIds.Contains(id)))
                await _calendarSharedRepository.CreateAsync(CalendarShared.CreatePrivate(id), ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure calendar shared records for calendars {CalendarIds}", string.Join(",", ids));
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(accountEmail, ct);

            // Prevent duplicate calendar names for the same account. This is a read-then-write
            // check: two concurrent creates of the same name can both pass it under Postgres's
            // default READ COMMITTED isolation, since a plain SELECT takes no lock. The
            // "Calendar_AccountEmail_Name_unique" constraint (caught below) is what actually
            // closes the race; this check only avoids the round trip in the common case.
            if (calendars.Any(c => c.Name == request.Name))
                return await FailAsync(ServiceResult.Conflict("Calendar.NameExists", "The calendar already exists."), ct);

            var calendarResult = Calendar.Create(accountEmail, request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (calendarResult.IsError)
                return await FailAsync(calendarResult.FirstError, ct);

            var calendar = calendarResult.Value;
            await _calendarRepository.CreateAsync(calendar, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex.IsPostgresUniqueViolation())
        {
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Conflict("Calendar.NameExists", "The calendar already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar {CalendarName} for account {AccountEmail}", request.Name, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(accountEmail, ct);
            Calendar? previous = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (previous == null)
                return await FailAsync(ServiceResult.NotFound("Calendar.NotFound", "The calendar could not be found."), ct);

            var updateResult = previous.Update(request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (updateResult.IsError)
                return await FailAsync(updateResult.FirstError, ct);

            await _calendarRepository.UpdateEntityAsync(previous, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex.IsPostgresUniqueViolation())
        {
            // Renaming to a name already used by another of this account's calendars hits the
            // same "Calendar_AccountEmail_Name_unique" constraint CreateCalendarAsync guards
            // against -- report the same friendly conflict instead of the generic fallback below.
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Conflict("Calendar.NameExists", "The calendar already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar {CalendarId} for account {AccountEmail}", request.Id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(accountEmail, ct);
            Calendar? existing = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (existing == null)
                return await FailAsync(ServiceResult.NotFound("Calendar.NotFound", "The calendar could not be found."), ct);

            await _calendarRepository.DeleteByIdAsync(existing.Id, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar {CalendarId} for account {AccountEmail}", request.Id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureDefaultCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(accountEmail, ct);

            // Create the default calendar only when the user has none yet (e.g., first login).
            if (calendars.Count != 0)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Ok();
            }

            // This first-visit auto-provisioning must always succeed so the calendar grid is never
            // permanently empty — fall back to UTC if the account's own TimeZoneIanaId is missing
            // or unrecognized, rather than letting Calendar.Create's normal (and deliberately
            // strict) validation silently fail an action the user didn't request.
            string timeZoneIanaId = request.TimeZoneIanaId ?? "";
            if (TimeZoneId.Create(timeZoneIanaId).IsError)
                timeZoneIanaId = "UTC";

            var calendarResult = Calendar.Create(accountEmail, request.Name, request.Description, timeZoneIanaId, request.HtmlColorCode);
            if (calendarResult.IsError)
                return await FailAsync(calendarResult.FirstError, ct);

            var calendar = calendarResult.Value;
            await _calendarRepository.CreateAsync(calendar, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex) when (ex.IsPostgresUniqueViolation())
        {
            // A concurrent request (e.g. two tabs opened on first login) already created this
            // account's default calendar. This method's contract is "some calendar now exists" --
            // already satisfied -- so treat the race as success rather than surfacing a failure.
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default calendar for account {AccountEmail}", accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            // A subscription must only ever be created for a calendar an admin has explicitly
            // marked shared to Users (CalendarShared.User) — otherwise any account could grant
            // itself read access to any calendar's events by supplying an arbitrary CalendarId.
            HashSet<long> shareableCalendarIds =
            [
                .. (await _calendarSharedRepository.GetAllAsync(ct))
                .Where(shared => shared.User)
                .Select(shared => shared.Id)
            ];

            // Replace-all strategy: delete existing subscriptions then re-insert from the new list.
            await _otherCalendarRepository.DeleteByAccountEmailAsync(email, ct);

            foreach (OtherCalendarRequest req in requests)
            {
                if (!shareableCalendarIds.Contains(req.CalendarId))
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Failure("Calendar.UpdateOtherCalendarsFailed", "Input is invalid");
                }

                var createResult = OtherCalendar.Create(email, req.CalendarId);
                if (createResult.IsError)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.FromError(createResult.FirstError);
                }
                await _otherCalendarRepository.CreateAsync(createResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update other-calendar subscriptions for account {AccountEmail}", email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("Calendar.UpdateOtherCalendarsFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarSharedAsync(string email, IEnumerable<CalendarSharedRequest> requests, CancellationToken ct = default)
    {
        var calendarSharedRequests = requests.ToList();

        List<Calendar> ownedCalendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);
        if (calendarSharedRequests.Any(req => ownedCalendars.All(c => c.Id != req.CalendarId)))
        {
            return ServiceResult.Validation("CalendarShared.InvalidTarget", "Input is invalid");
        }

        await _unitOfWork.BeginAsync(ct);
        try
        {
            // Upsert: a CalendarShared row may not exist yet for a calendar being shared for the
            // first time (EnsureCalendarSharedAsync is not guaranteed to have run). UpdateEntityAsync
            // throws when the row is missing, which previously failed the whole request.
            HashSet<long> existingShareIds = [.. (await _calendarSharedRepository.GetAllAsync(ct)).Select(s => s.Id)];

            foreach (CalendarSharedRequest req in calendarSharedRequests)
            {
                CalendarShared shared = CalendarShared.CreatePrivate(req.CalendarId).Update(req.User, req.Anonymous);
                if (existingShareIds.Contains(req.CalendarId))
                    await _calendarSharedRepository.UpdateEntityAsync(shared, ct);
                else
                    await _calendarSharedRepository.CreateAsync(shared, ct);
            }

            // When a calendar is set to non-user-visible, remove ALL existing subscriptions to it
            // (every account that had subscribed), not just one — otherwise leftover subscription
            // rows silently reactivate the moment the calendar is shared again. One batched DELETE
            // per calendar instead of one round trip per subscriber.
            foreach (CalendarSharedRequest req in calendarSharedRequests.Where(r => !r.User))
            {
                await _otherCalendarRepository.DeleteByCalendarIdAsync(req.CalendarId, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar shared settings");
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("Calendar.UpdateSharedFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderDto> reminders, AttachedFileDto? attachedFile, CancellationToken ct = default)
    {
        var fileError = _attachmentContent.ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = _attachmentContent.SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);

            // Verify the target calendar belongs to the requesting user.
            if (calendars.All(c => c.Id != request.CalendarId))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.NotFound("Calendar.NotFound", "The calendar does not exists.");
            }

            var eventResult = CalendarEvent.Create(request.CalendarId, request.Title, description,
                request.AllDay, request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status, request.RecurrenceId);
            if (eventResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.FromError(eventResult.FirstError);
            }

            var calendarEvent = eventResult.Value;
            long eventId = await _calendarEventRepository.CreateCalendarEventAsync(calendarEvent, ct);

            if (attachedFile != null)
            {
                var attachmentResult = await SaveEventAttachmentAsync(roleIndex, eventId, attachedFile, ct);
                if (!attachmentResult.Success)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return attachmentResult;
                }
            }

            var remindersResult = await CreateEventRemindersAsync(eventId, reminders, ct);
            if (!remindersResult.Success)
            {
                await _unitOfWork.RollbackAsync(ct);
                return remindersResult;
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event for calendar {CalendarId} and account {AccountEmail}", request.CalendarId, email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("CalendarEvent.CreateFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderDto> reminders, AttachedFileDto? attachedFile, CancellationToken ct = default)
    {
        var fileError = _attachmentContent.ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = _attachmentContent.SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);

            // Verify the target calendar belongs to the requesting user.
            if (calendars.All(c => c.Id != request.CalendarId))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.NotFound("Calendar.NotFound", "The calendar does not exists.");
            }

            CalendarEvent? existing = await _calendarEventRepository.FindByIdAsync(request.Id, ct);
            // Verify the existing event currently belongs to one of the caller's calendars
            // before allowing any mutation or reassignment.
            if (existing == null || existing.Id <= 0 || calendars.All(c => c.Id != existing.CalendarId))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Validation("CalendarEvent.InvalidRequest", "Input is invalid");
            }

            var updateResult = existing.Update(request.Title, description, request.AllDay,
                request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.FromError(updateResult.FirstError);
            }

            existing.Reassign(request.CalendarId, request.RecurrenceId);
            await _calendarEventRepository.UpdateEntityAsync(existing, ct);

            // Replace attachment: delete existing record then optionally upload and re-create.
            await _calendarEventAttachedFileRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            if (attachedFile != null)
            {
                var attachmentResult = await SaveEventAttachmentAsync(roleIndex, existing.Id, attachedFile, ct);
                if (!attachmentResult.Success)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return attachmentResult;
                }
            }

            // Replace reminders: delete all existing then re-create from the updated list.
            await _calendarEventReminderRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            var remindersResult = await CreateEventRemindersAsync(existing.Id, reminders, ct);
            if (!remindersResult.Success)
            {
                await _unitOfWork.RollbackAsync(ct);
                return remindersResult;
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event {EventId} for account {AccountEmail}", request.Id, email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("CalendarEvent.UpdateFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);
            if (calendars.Count == 0)
                return await FailAsync(ServiceResult.NotFound("Calendar.None", "No calendar exists."), ct);

            CalendarEvent? evt = await _calendarEventRepository.FindByIdAsync(calendarEventId, ct);

            // Ensure the event exists and belongs to one of the requesting user's calendars.
            if (evt == null || calendars.All(c => c.Id != evt.CalendarId))
                return await FailAsync(ServiceResult.NotFound("CalendarEvent.NotFound", "The calendar event could not be found."), ct);

            await _calendarEventRepository.DeleteByIdAsync(evt.Id, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for account {AccountEmail}", calendarEventId, email);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    private static ServiceResult UnexpectedFailure => ServiceResult.Failure(
        "Calendar.Unexpected", "The operation could not be completed. Please try again.");

    /// <summary>Rolls back the current transaction and returns <paramref name="result"/>.</summary>
    private async Task<ServiceResult> FailAsync(ServiceResult result, CancellationToken ct)
    {
        await _unitOfWork.RollbackAsync(ct);
        return result;
    }

    /// <summary>Rolls back the current transaction and returns a failure mapped from <paramref name="error"/>.</summary>
    private Task<ServiceResult> FailAsync(ErrorOr.Error error, CancellationToken ct) => FailAsync(ServiceResult.FromError(error), ct);

    /// <summary>
    /// Builds the remote storage path for a calendar event attachment.
    /// Uses a GUID prefix to avoid collisions and isolate files per event.
    /// </summary>
    private static string BuildEventFilePath(string roleIndex, long eventId, string fileName)
    {
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", "Calendar", roleIndex, "calendarEventAttachedFiles", $"{eventId}", $"{guid}{ext}");
    }

    /// <summary>
    /// Uploads <paramref name="attachedFile"/> to file storage and creates its
    /// <see cref="CalendarEventAttachedFile"/> record for <paramref name="calendarEventId"/>.
    /// Used by both create and update (the caller is responsible for deleting any prior
    /// attachment record before calling this on update).
    /// </summary>
    private async Task<ServiceResult> SaveEventAttachmentAsync(string roleIndex, long calendarEventId, AttachedFileDto attachedFile, CancellationToken ct)
    {
        string filePath = BuildEventFilePath(roleIndex, calendarEventId, attachedFile.FileName);
        bool uploaded = await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
        if (!uploaded)
            return ServiceResult.Failure("CalendarEvent.AttachmentUploadFailed", "Input is invalid");

        var fileResult = CalendarEventAttachedFile.Create(calendarEventId, attachedFile.Size,
            Path.GetFileNameWithoutExtension(attachedFile.FileName),
            Path.GetExtension(attachedFile.FileName),
            filePath.Replace('\\', '/')); // Normalize path separators for URL use.
        if (fileResult.IsError)
            return ServiceResult.FromError(fileResult.FirstError);

        await _calendarEventAttachedFileRepository.CreateAsync(fileResult.Value, ct);
        return ServiceResult.Ok();
    }

    /// <summary>
    /// Creates a <see cref="CalendarEventReminder"/> for each entry in <paramref name="reminders"/> for
    /// <paramref name="calendarEventId"/>. Any invalid reminder (empty method, wrong lead-time field
    /// count, or an out-of-range lead time — see CalendarEventReminder.Create / CalendarReminderPolicy)
    /// stops immediately so the caller can roll back instead of silently dropping a reminder.
    /// </summary>
    private async Task<ServiceResult> CreateEventRemindersAsync(long calendarEventId, List<CalendarReminderDto> reminders, CancellationToken ct)
    {
        foreach (var reminderDto in reminders)
        {
            var reminderResult = CalendarEventReminder.Create(calendarEventId, reminderDto.Method,
                reminderDto.MinutesBeforeEvent, reminderDto.HoursBeforeEvent, reminderDto.DaysBeforeEvent, reminderDto.WeeksBeforeEvent,
                reminderDto.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(reminderDto.TimesBeforeEvent.Value) : null);
            if (reminderResult.IsError)
                return ServiceResult.FromError(reminderResult.FirstError);

            await _calendarEventReminderRepository.CreateAsync(reminderResult.Value, ct);
        }
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileDto file, string roleIndex, CancellationToken ct = default)
        => _attachmentContent.UploadSummernoteImageAsync(file, "Calendar", roleIndex, ct);

    /// <inheritdoc />
    public Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken ct = default)
        => _attachmentContent.DownloadFileAsync(filePath, ct);

    /// <inheritdoc />
    public Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct = default)
        => _attachmentContent.PrepareHtmlForDisplayAsync(html, ct);
}
