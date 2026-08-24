using Maroik.Core.Domain.Calendar;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
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
        =>
        [
            .. (await _otherCalendarRepository.GetByAccountEmailAsync(email, ct)).Select(OtherCalendarMapper.ToResponse)
        ];

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync(CancellationToken ct = default)
        => [.. (await _calendarSharedRepository.GetAllAsync(ct)).Select(CalendarSharedMapper.ToResponse)];

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureCalendarSharedAsync(IEnumerable<long> calendarIds, CancellationToken ct = default)
    {
        List<long> ids = [.. calendarIds];
        try
        {
            // Single fetch of the whole table, reused for every ID instead of one full scan per calendar.
            List<CalendarShared> shares = await _calendarSharedRepository.GetAllAsync(ct);
            HashSet<long> existingIds = [.. shares.Select(s => s.Id)];

            foreach (long id in ids.Where(id => !existingIds.Contains(id)))
                await _calendarSharedRepository.CreateAsync(CalendarShared.CreatePrivate(id), ct);

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure calendar shared records for calendars {CalendarIds}", string.Join(",", ids));
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(request.AccountEmail ?? "", ct);

            // Prevent duplicate calendar names for the same account.
            if (calendars.Any(c => c.Name == request.Name))
                return ServiceResult.Fail("The calendar already exists.");

            var calendarResult = Calendar.Create(request.AccountEmail, request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (calendarResult.IsError)
                return ServiceResult.Fail(calendarResult.FirstError.Description);

            var calendar = calendarResult.Value;
            await _calendarRepository.CreateAsync(calendar, ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar {CalendarName} for account {AccountEmail}", request.Name, request.AccountEmail);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(request.AccountEmail ?? "", ct);
            Calendar? previous = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (previous == null)
                return ServiceResult.Fail("Input is invalid");

            var updateResult = previous.Update(request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (updateResult.IsError)
                return ServiceResult.Fail(updateResult.FirstError.Description);

            await _calendarRepository.UpdateEntityAsync(previous, ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar {CalendarId}", request.Id);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(request.AccountEmail ?? "", ct);
            Calendar? existing = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (existing == null)
                return ServiceResult.Fail("Input is invalid");

            await _calendarRepository.DeleteByIdAsync(existing.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar {CalendarId}", request.Id);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(request.AccountEmail ?? "", ct);

            // Create the default calendar only when the user has none yet (e.g., first login).
            if (calendars.Count != 0)
            {
                return ServiceResult.Ok();
            }

            // This first-visit auto-provisioning must always succeed so the calendar grid is never
            // permanently empty — fall back to UTC if the account's own TimeZoneIanaId is missing
            // or unrecognized, rather than letting Calendar.Create's normal (and deliberately
            // strict) validation silently fail an action the user didn't request.
            string timeZoneIanaId = request.TimeZoneIanaId ?? "";
            try
            {
                _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneIanaId);
            }
            catch (Exception)
            {
                timeZoneIanaId = "UTC";
            }

            var calendarResult = Calendar.Create(request.AccountEmail, request.Name, request.Description, timeZoneIanaId, request.HtmlColorCode);
            if (calendarResult.IsError)
                return ServiceResult.Fail(calendarResult.FirstError.Description);

            var calendar = calendarResult.Value;
            await _calendarRepository.CreateAsync(calendar, ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure default calendar for account {AccountEmail}", request.AccountEmail);
            return ServiceResult.Fail("Input is invalid");
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
                    return ServiceResult.Fail("Input is invalid");
                }

                var createResult = OtherCalendar.Create(email, req.CalendarId);
                if (createResult.IsError)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail(createResult.FirstError.Description);
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
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarSharedAsync(string email, IEnumerable<CalendarSharedRequest> requests, CancellationToken ct = default)
    {
        var calendarSharedRequests = requests.ToList();

        List<Calendar> ownedCalendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);
        if (calendarSharedRequests.Any(req => ownedCalendars.All(c => c.Id != req.CalendarId)))
        {
            return ServiceResult.Fail("Input is invalid");
        }

        await _unitOfWork.BeginAsync(ct);
        try
        {
            foreach (var shared in calendarSharedRequests.Select(req => CalendarShared.CreatePrivate(req.CalendarId).Update(req.User, req.Anonymous)))
            {
                await _calendarSharedRepository.UpdateEntityAsync(shared, ct);
            }

            List<OtherCalendar> allOtherCalendars = await _otherCalendarRepository.GetAllAsync(ct);

            // When a calendar is set to non-user-visible, remove ALL existing subscriptions to it
            // (every account that had subscribed), not just one — otherwise leftover subscription
            // rows silently reactivate the moment the calendar is shared again.
            foreach (CalendarSharedRequest req in calendarSharedRequests.Where(r => !r.User))
            {
                foreach (OtherCalendar other in allOtherCalendars.Where(o => o.CalendarId == req.CalendarId))
                {
                    try
                    {
                        await _otherCalendarRepository.DeleteByAccountEmailAndCalendarIdAsync(other.AccountEmail.Value, other.CalendarId, ct);
                    }
                    catch (Exception ex)
                    {
                        // Best effort: a stale subscription is not worth failing the whole request over,
                        // but a swallowed failure here (transient or otherwise) should still be visible.
                        _logger.LogWarning(ex, "Failed to remove stale calendar subscription for {AccountEmail} to calendar {CalendarId}",
                            other.AccountEmail.Value, other.CalendarId);
                    }
                }
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar shared settings");
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
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
                return ServiceResult.Fail("The calendar does not exists.");
            }

            var eventResult = CalendarEvent.Create(request.CalendarId, request.Title, description,
                request.AllDay, request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status, request.RecurrenceId);
            if (eventResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(eventResult.FirstError.Description);
            }

            var calendarEvent = eventResult.Value;
            long eventId = await _calendarEventRepository.CreateCalendarEventAsync(calendarEvent, ct);

            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, eventId, attachedFile.FileName);
                bool uploaded = await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
                if (!uploaded)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Input is invalid");
                }
                var fileResult = CalendarEventAttachedFile.Create(eventId, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/')); // Normalize path separators for URL use.
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.CreateAsync(fileResult.Value, ct);
            }

            // Create all reminder entries for the event within the same transaction. A malformed
            // reminder is skipped rather than failing the whole save — matching main, where the
            // event (and its other, valid reminders) is never blocked by one bad reminder.
            foreach (var reminderResult in reminders.Select(r => CalendarEventReminder.Create(eventId, r.Method,
                         r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                         r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null)))
            {
                if (reminderResult.IsError)
                {
                    _logger.LogWarning("Skipped invalid reminder on calendar event {EventId}: {Error}", eventId, reminderResult.FirstError.Description);
                    continue;
                }
                await _calendarEventReminderRepository.CreateAsync(reminderResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create calendar event for calendar {CalendarId} and account {AccountEmail}", request.CalendarId, email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
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
                return ServiceResult.Fail("The calendar does not exists.");
            }

            CalendarEvent? existing = await _calendarEventRepository.FindByIdAsync(request.Id, ct);
            // Verify the existing event currently belongs to one of the caller's calendars
            // before allowing any mutation or reassignment.
            if (existing == null || existing.Id <= 0 || calendars.All(c => c.Id != existing.CalendarId))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            var updateResult = existing.Update(request.Title, description, request.AllDay,
                request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            existing.Reassign(request.CalendarId, request.RecurrenceId);
            await _calendarEventRepository.UpdateEntityAsync(existing, ct);

            // Replace attachment: delete existing record then optionally upload and re-create.
            await _calendarEventAttachedFileRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, existing.Id, attachedFile.FileName);
                bool uploaded = await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? "", ct);
                if (!uploaded)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Input is invalid");
                }
                var fileResult = CalendarEventAttachedFile.Create(existing.Id, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/'));
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.CreateAsync(fileResult.Value, ct);
            }

            // Replace reminders: delete all existing then re-create from the updated list. A
            // malformed reminder is skipped rather than failing the whole save — matching main,
            // where the event update is never blocked by one bad reminder.
            await _calendarEventReminderRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            foreach (var reminderResult in reminders.Select(r => CalendarEventReminder.Create(existing.Id, r.Method,
                         r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                         r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null)))
            {
                if (reminderResult.IsError)
                {
                    _logger.LogWarning("Skipped invalid reminder on calendar event {EventId}: {Error}", existing.Id, reminderResult.FirstError.Description);
                    continue;
                }
                await _calendarEventReminderRepository.CreateAsync(reminderResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update calendar event {EventId} for account {AccountEmail}", request.Id, email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);
            if (calendars.Count == 0)
                return ServiceResult.Fail("No calendar exists");

            CalendarEvent? evt = await _calendarEventRepository.FindByIdAsync(calendarEventId, ct);

            // Ensure the event exists and belongs to one of the requesting user's calendars.
            if (evt == null || calendars.All(c => c.Id != evt.CalendarId))
                return ServiceResult.Fail("Input is invalid");

            await _calendarEventRepository.DeleteByIdAsync(evt.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete calendar event {EventId} for account {AccountEmail}", calendarEventId, email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

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
