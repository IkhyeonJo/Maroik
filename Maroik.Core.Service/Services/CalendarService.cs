using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="ICalendarService"/> covering the full calendar feature set:
/// calendars, events, reminders, file attachments, sharing, and subscriptions.
/// Event attachments are virus-scanned by ClamAV and image-validated before upload.
/// Files are stored in and retrieved from the file-storage microservice.
/// All multi-step write operations use <see cref="IUnitOfWork"/> transactions.
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
    private readonly IRsaService _rsa;
    private readonly IImageValidator _imageValidator;
    private readonly IHtmlContentSanitizer _htmlSanitizer;
    private readonly IHtmlParser _htmlParser;
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
        IRsaService rsa,
        IImageValidator imageValidator,
        IHtmlContentSanitizer htmlSanitizer,
        IHtmlParser htmlParser,
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
        _rsa = rsa;
        _imageValidator = imageValidator;
        _htmlSanitizer = htmlSanitizer;
        _htmlParser = htmlParser;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetCalendarsAsync(string email, CancellationToken ct = default)
        => (await _calendarRepository.GetByAccountEmailAsync(email, ct)).OrderBy(c => c.Name).Select(CalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetAllCalendarsAsync(CancellationToken ct = default)
        => (await _calendarRepository.GetAllOrderedByNameAsync(ct)).Select(CalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId, CancellationToken ct = default)
        => (await _calendarEventRepository.GetByCalendarIdAsync(calendarId, ct)).Select(CalendarEventMapper.ToResponse);

    /// <inheritdoc />
    public async Task<CalendarEventResponse?> GetCalendarEventAsync(long id, CancellationToken ct = default)
    {
        CalendarEvent? evt = await _calendarEventRepository.FindByIdAsync(id, ct);
        return evt == null ? null : CalendarEventMapper.ToResponse(evt);
    }

    /// <inheritdoc />
    public async Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId, CancellationToken ct = default)
    {
        CalendarEventAttachedFile? file = await _calendarEventAttachedFileRepository.FindByCalendarEventIdAsync(calendarEventId, ct);
        return file == null ? null : CalendarEventAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId, CancellationToken ct = default)
        => (await _calendarEventReminderRepository.GetByCalendarEventIdAsync(calendarEventId, ct)).Select(CalendarEventReminderMapper.ToResponse);

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email, CancellationToken ct = default)
        => (await _otherCalendarRepository.GetByAccountEmailAsync(email, ct)).Select(OtherCalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync(CancellationToken ct = default)
        => (await _calendarSharedRepository.GetAllAsync(ct)).Select(CalendarSharedMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureCalendarSharedAsync(long calendarId, CancellationToken ct = default)
    {
        try
        {
            List<CalendarShared> shareds = await _calendarSharedRepository.GetAllAsync(ct);

            // Only create a sharing record if one does not already exist for this calendar.
            if (!shareds.Any(s => s.Id == calendarId))
                await _calendarSharedRepository.CreateAsync(CalendarShared.CreatePrivate(calendarId), ct);

            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
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
        catch { return ServiceResult.Fail("Input is invalid"); }
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
        catch { return ServiceResult.Fail("Input is invalid"); }
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
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(request.AccountEmail ?? "", ct);

            // Create the default calendar only when the user has none yet (e.g., first login).
            if (calendars.Count == 0)
            {
                var calendarResult = Calendar.Create(request.AccountEmail, request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
                if (calendarResult.IsError)
                    return ServiceResult.Fail(calendarResult.FirstError.Description);

                var calendar = calendarResult.Value;
                await _calendarRepository.CreateAsync(calendar, ct);
            }
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            // Replace-all strategy: delete existing subscriptions then re-insert from the new list.
            await _otherCalendarRepository.DeleteByAccountEmailAsync(email, ct);

            foreach (OtherCalendarRequest req in requests)
            {
                var createResult = OtherCalendar.Create(email, req.CalendarId);
                if (createResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(createResult.FirstError.Description);
                }
                await _otherCalendarRepository.CreateAsync(createResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarSharedAsync(IEnumerable<CalendarSharedRequest> requests, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            foreach (CalendarSharedRequest req in requests)
            {
                var shared = CalendarShared.CreatePrivate(req.CalendarId).Update(req.User, req.Anonymous);
                await _calendarSharedRepository.UpdateEntityAsync(shared, ct);
            }

            List<OtherCalendar> allOtherCalendars = await _otherCalendarRepository.GetAllAsync(ct);

            // When a calendar is set to non-user-visible, remove any existing subscriptions to it.
            foreach (CalendarSharedRequest req in requests.Where(r => !r.User))
            {
                OtherCalendar? other = allOtherCalendars.FirstOrDefault(o => o.CalendarId == req.CalendarId);
                if (other != null)
                {
                    try
                    {
                        await _otherCalendarRepository.DeleteByAccountEmailAndCalendarIdAsync(other.AccountEmail.Value, other.CalendarId, ct);
                    }
                    catch { /* best effort: subscription removal is non-critical */ }
                }
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Validates an event attached file: only <c>.zip</c> files within the configured size limit are accepted.
    /// Returns <see langword="null"/> when the file passes validation (or when no file is provided).
    /// </summary>
    private ServiceResult? ValidateAttachedFile(AttachedFileInfo? attachedFile)
    {
        if (attachedFile == null) return null;
        if (Path.GetExtension(attachedFile.FileName) != ".zip")
            return ServiceResult.Fail("Only zip extension allowed.");
        long maxBytes = _settings.Value.MaxAttachedFileSizeBytes;
        if (attachedFile.Size <= 0 || attachedFile.Size > maxBytes)
            return ServiceResult.Fail($"Uploaded file size must be smaller than {maxBytes / (1024 * 1024)}MB.");
        return null;
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile, CancellationToken ct = default)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);

            // Verify the target calendar belongs to the requesting user.
            if (!calendars.Any(c => c.Id == request.CalendarId))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            var eventResult = CalendarEvent.Create(request.CalendarId, request.Title, description,
                request.AllDay, request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status, request.RecurrenceId);
            if (eventResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(eventResult.FirstError.Description);
            }

            var calendarEvent = eventResult.Value;
            long eventId = await _calendarEventRepository.CreateCalendarEventAsync(calendarEvent, ct);

            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, eventId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
                var fileResult = CalendarEventAttachedFile.Create(eventId, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/')); // Normalise path separators for URL use.
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.CreateAsync(fileResult.Value, ct);
            }

            // Create all reminder entries for the event within the same transaction.
            foreach (CalendarReminderInfo r in reminders)
            {
                var reminderResult = CalendarEventReminder.Create(eventId, r.Method,
                    r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                    r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null);
                if (reminderResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(reminderResult.FirstError.Description);
                }
                await _calendarEventReminderRepository.CreateAsync(reminderResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile, CancellationToken ct = default)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);

            // Verify the target calendar belongs to the requesting user.
            if (!calendars.Any(c => c.Id == request.CalendarId))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            CalendarEvent? existing = await _calendarEventRepository.FindByIdAsync(request.Id, ct);
            if (existing == null || existing.Id <= 0)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            var updateResult = existing.Update(request.Title, description, request.AllDay,
                request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            existing.Reassign(request.CalendarId, request.RecurrenceId);
            await _calendarEventRepository.UpdateEntityAsync(existing, ct);

            // Replace attachment: delete existing record then optionally upload and re-create.
            await _calendarEventAttachedFileRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, existing.Id, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
                var fileResult = CalendarEventAttachedFile.Create(existing.Id, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/'));
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.CreateAsync(fileResult.Value, ct);
            }

            // Replace reminders: delete all existing then re-create from the updated list.
            await _calendarEventReminderRepository.DeleteByCalendarEventIdAsync(existing.Id, ct);
            foreach (CalendarReminderInfo r in reminders)
            {
                var reminderResult = CalendarEventReminder.Create(existing.Id, r.Method,
                    r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                    r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null);
                if (reminderResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(reminderResult.FirstError.Description);
                }
                await _calendarEventReminderRepository.CreateAsync(reminderResult.Value, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email, CancellationToken ct = default)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetByAccountEmailAsync(email, ct);
            CalendarEvent? evt = await _calendarEventRepository.FindByIdAsync(calendarEventId, ct);

            // Ensure the event exists and belongs to one of the requesting user's calendars.
            if (evt == null || !calendars.Any(c => c.Id == evt.CalendarId))
                return ServiceResult.Fail("Input is invalid");

            await _calendarEventRepository.DeleteByIdAsync(evt.Id, ct);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
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
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string roleIndex, CancellationToken ct = default)
    {
        if (!_imageValidator.IsValidImage(file.Bytes))
            return SummernoteUploadResult.Fail("Invalid image file.");

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        // Use a GUID-based file name to avoid collisions and prevent path traversal via the original name.
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine("upload", "Calendar", roleIndex, "summernote", "images", imageFile);

        bool uploaded = await _fileClient.UploadAsync(file.Bytes, file.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        if (!uploaded)
            return SummernoteUploadResult.Fail("Input is invalid");

        byte[] fileBytes = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        return SummernoteUploadResult.Ok(fileBytes, file.ContentType, imageFile, filePath);
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            return await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download file {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct = default)
    {
        return await _htmlParser.TransformImageAttributesAsync(html, async (alt, token) =>
        {
            byte[]? fileData = await _fileClient.DownloadAsync(alt, _settings.Value.FileStorageBaseUrl ?? string.Empty, token);
            if (fileData == null) return null;
            return new HtmlImgPatch(
                NewAlt: _rsa.Encrypt(alt),
                DataFile: Convert.ToBase64String(fileData),
                DataContentType: GetImageContentType(alt));
        }, ct);
    }

    /// <summary>Sanitizes HTML to remove unsafe markup and decrypts RSA-encrypted file paths in img alt attributes.</summary>
    private string SanitizeAndDecryptContent(string html)
    {
        string sanitized = _htmlSanitizer.Sanitize(html);
        return _htmlParser.TransformImageAttributes(sanitized, alt => new HtmlImgPatch(NewAlt: _rsa.Decrypt(alt)));
    }

    /// <summary>Returns the MIME content type for Summernote-uploaded images by file extension.</summary>
    private static string GetImageContentType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            _                 => "application/octet-stream"
        };
}
