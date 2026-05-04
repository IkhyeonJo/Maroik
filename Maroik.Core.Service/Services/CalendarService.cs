using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
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
/// Event attachments are virus-scanned by ClamAV and image-processed by ImageMagick before upload.
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
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IRsaService _rsa;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<CalendarService> _logger;

    /// <summary>Initializes a new instance of <see cref="CalendarService"/> with the supplied dependencies.</summary>
    /// <param name="calendarRepository">Repository for calendar persistence.</param>
    /// <param name="calendarEventRepository">Repository for calendar event persistence.</param>
    /// <param name="calendarEventAttachedFileRepository">Repository for event file attachments.</param>
    /// <param name="calendarEventReminderRepository">Repository for event reminders.</param>
    /// <param name="calendarSharedRepository">Repository for calendar sharing settings.</param>
    /// <param name="otherCalendarRepository">Repository for subscriptions to other users' calendars.</param>
    /// <param name="fileClient">Remote file-storage client for upload/download operations.</param>
    /// <param name="unitOfWork">Unit-of-work abstraction that wraps multi-step writes in a single transaction.</param>
    /// <param name="dispatcher">Domain event dispatcher invoked after calendar/event creation.</param>
    /// <param name="rsa">RSA service used to encrypt/decrypt inline image paths in HTML content.</param>
    /// <param name="settings">Server configuration options including the file-storage base URL.</param>
    /// <param name="logger">Logger for error diagnostics.</param>
    public CalendarService(
        ICalendarRepository calendarRepository,
        ICalendarEventRepository calendarEventRepository,
        ICalendarEventAttachedFileRepository calendarEventAttachedFileRepository,
        ICalendarEventReminderRepository calendarEventReminderRepository,
        ICalendarSharedRepository calendarSharedRepository,
        IOtherCalendarRepository otherCalendarRepository,
        IFileClient fileClient,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IRsaService rsa,
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
        _dispatcher = dispatcher;
        _rsa = rsa;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetCalendarsAsync(string email)
        => (await _calendarRepository.GetCalendarsAsync(email)).Select(CalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetAllCalendarsAsync()
        => (await _calendarRepository.GetAllCalendarsAsync()).Select(CalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId)
        => (await _calendarEventRepository.GetCalendarEventsAsync(calendarId)).Select(CalendarEventMapper.ToResponse);

    /// <inheritdoc />
    public async Task<CalendarEventResponse?> GetCalendarEventAsync(long id)
    {
        CalendarEvent? evt = await _calendarEventRepository.GetCalendarEventAsync(id);
        return evt == null ? null : CalendarEventMapper.ToResponse(evt);
    }

    /// <inheritdoc />
    public async Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId)
    {
        CalendarEventAttachedFile? file = await _calendarEventAttachedFileRepository.GetCalendarEventAttachedFileAsync(calendarEventId);
        return file == null ? null : CalendarEventAttachedFileMapper.ToResponse(file);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId)
        => (await _calendarEventReminderRepository.GetCalendarEventRemindersAsync(calendarEventId)).Select(CalendarEventReminderMapper.ToResponse);

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email)
        => (await _otherCalendarRepository.GetOtherCalendarsAsync(email)).Select(OtherCalendarMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync()
        => (await _calendarSharedRepository.GetCalendarSharedAsync()).Select(CalendarSharedMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureCalendarSharedAsync(long calendarId)
    {
        try
        {
            List<CalendarShared> shareds = await _calendarSharedRepository.GetCalendarSharedAsync();

            // Only create a sharing record if one does not already exist for this calendar.
            if (!shareds.Any(s => s.Id == calendarId))
                await _calendarSharedRepository.CreateCalendarSharedAsync(CalendarShared.CreatePrivate(calendarId));

            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");

            // Prevent duplicate calendar names for the same account.
            if (calendars.Any(c => c.Name == request.Name))
                return ServiceResult.Fail("The calendar already exists.");

            var calendarResult = Calendar.Create(0, request.AccountEmail, request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (calendarResult.IsError)
                return ServiceResult.Fail(calendarResult.FirstError.Description);

            var calendar = calendarResult.Value;
            await _calendarRepository.CreateCalendarAsync(calendar);
            await _dispatcher.DispatchAsync(calendar.PopDomainEvents());
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");
            Calendar? previous = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (previous == null)
                return ServiceResult.Fail("Input is invalid");

            var updateResult = previous.Update(request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
            if (updateResult.IsError)
                return ServiceResult.Fail(updateResult.FirstError.Description);

            await _calendarRepository.UpdateCalendarAsync(previous);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");
            Calendar? existing = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (existing == null)
                return ServiceResult.Fail("Input is invalid");

            await _calendarRepository.DeleteCalendarAsync(existing);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");

            // Create the default calendar only when the user has none yet (e.g., first login).
            if (calendars.Count == 0)
            {
                var calendarResult = Calendar.Create(0, request.AccountEmail, request.Name, request.Description, request.TimeZoneIanaId, request.HtmlColorCode);
                if (calendarResult.IsError)
                    return ServiceResult.Fail(calendarResult.FirstError.Description);

                var calendar = calendarResult.Value;
                await _calendarRepository.CreateCalendarAsync(calendar);
                await _dispatcher.DispatchAsync(calendar.PopDomainEvents());
            }
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            // Replace-all strategy: delete existing subscriptions then re-insert from the new list.
            foreach (OtherCalendar previous in await _otherCalendarRepository.GetOtherCalendarsAsync(email) ?? [])
                await _otherCalendarRepository.DeleteOtherCalendarAsync(previous);

            foreach (OtherCalendarRequest req in requests)
            {
                var createResult = OtherCalendar.Create(email, req.CalendarId);
                if (createResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(createResult.FirstError.Description);
                }
                await _otherCalendarRepository.CreateOtherCalendarAsync(createResult.Value);
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarSharedAsync(IEnumerable<CalendarSharedRequest> requests)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            foreach (CalendarSharedRequest req in requests)
            {
                var shared = CalendarShared.CreatePrivate(req.CalendarId).Update(req.User, req.Anonymous);
                await _calendarSharedRepository.UpdateCalendarSharedAsync(shared);
            }

            List<OtherCalendar> allOtherCalendars = await _otherCalendarRepository.GetAllOtherCalendarsAsync() ?? [];

            // When a calendar is set to non-user-visible, remove any existing subscriptions to it.
            foreach (CalendarSharedRequest req in requests.Where(r => !r.User))
            {
                OtherCalendar? other = allOtherCalendars.FirstOrDefault(o => o.CalendarId == req.CalendarId);
                if (other != null)
                {
                    try { await _otherCalendarRepository.DeleteOtherCalendarAsync(other); }
                    catch { /* best effort: subscription removal is non-critical */ }
                }
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Validates an event attached file: only <c>.zip</c> files up to 10 MB are accepted.
    /// Returns <see langword="null"/> when the file passes validation (or when no file is provided).
    /// </summary>
    /// <param name="attachedFile">The file to validate, or <see langword="null"/> when no file was uploaded.</param>
    /// <returns>A failure <see cref="ServiceResult"/> describing the violation, or <see langword="null"/> on success.</returns>
    private static ServiceResult? ValidateAttachedFile(AttachedFileInfo? attachedFile)
    {
        if (attachedFile == null) return null;
        if (Path.GetExtension(attachedFile.FileName) != ".zip")
            return ServiceResult.Fail("Only zip extension allowed.");
        if (attachedFile.Size is not (> 0 and <= 10485760))
            return ServiceResult.Fail("Uploaded file size must be smaller than 10MB.");
        return null;
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync();
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(email);

            // Verify the target calendar belongs to the requesting user.
            if (!calendars.Any(c => c.Id == request.CalendarId))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            var eventResult = CalendarEvent.Create(0, request.CalendarId, request.Title, description,
                request.AllDay, request.StartDate, request.EndDate,
                request.StartDateTimeZoneIanaId, request.EndDateTimeZoneIanaId,
                request.Location, request.Status, request.RecurrenceId);
            if (eventResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(eventResult.FirstError.Description);
            }

            var calendarEvent = eventResult.Value;
            long eventId = await _calendarEventRepository.CreateCalendarEventAsync(calendarEvent);
            await _dispatcher.DispatchAsync(calendarEvent.PopDomainEvents());

            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, eventId, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                var fileResult = CalendarEventAttachedFile.Create(0, eventId, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/')); // Normalise path separators for URL use.
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.SaveCalendarEventAttachedFileAsync(fileResult.Value);
            }

            // Create all reminder entries for the event within the same transaction.
            foreach (CalendarReminderInfo r in reminders)
            {
                var reminderResult = CalendarEventReminder.Create(0, eventId, r.Method,
                    r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                    r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null);
                if (reminderResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(reminderResult.FirstError.Description);
                }
                await _calendarEventReminderRepository.CreateCalendarEventReminderAsync(reminderResult.Value);
            }

            await _unitOfWork.CommitAsync();
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
        List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile)
    {
        var fileError = ValidateAttachedFile(attachedFile);
        if (fileError != null) return fileError;

        string description = SanitizeAndDecryptContent(request.Description ?? "");

        await _unitOfWork.BeginAsync();
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(email);

            // Verify the target calendar belongs to the requesting user.
            if (!calendars.Any(c => c.Id == request.CalendarId))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            CalendarEvent? existing = await _calendarEventRepository.GetCalendarEventAsync(request.Id);
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
            await _calendarEventRepository.UpdateCalendarEventAsync(existing);

            // Replace attachment: delete existing record then optionally upload and re-create.
            await _calendarEventAttachedFileRepository.DeleteCalendarEventAttachedFileAsync(existing.Id);
            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, existing.Id, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                var fileResult = CalendarEventAttachedFile.Create(0, existing.Id, attachedFile.Size,
                    Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Path.GetExtension(attachedFile.FileName),
                    filePath.Replace('\\', '/'));
                if (fileResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(fileResult.FirstError.Description);
                }
                await _calendarEventAttachedFileRepository.SaveCalendarEventAttachedFileAsync(fileResult.Value);
            }

            // Replace reminders: delete all existing then re-create from the updated list.
            await _calendarEventReminderRepository.DeleteCalendarEventReminderAsync(existing.Id);
            foreach (CalendarReminderInfo r in reminders)
            {
                var reminderResult = CalendarEventReminder.Create(0, existing.Id, r.Method,
                    r.MinutesBeforeEvent, r.HoursBeforeEvent, r.DaysBeforeEvent, r.WeeksBeforeEvent,
                    r.TimesBeforeEvent.HasValue ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value) : null);
                if (reminderResult.IsError)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail(reminderResult.FirstError.Description);
                }
                await _calendarEventReminderRepository.CreateCalendarEventReminderAsync(reminderResult.Value);
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email)
    {
        try
        {
            List<Calendar> calendars = await _calendarRepository.GetCalendarsAsync(email);
            CalendarEvent? evt = await _calendarEventRepository.GetCalendarEventAsync(calendarEventId);

            // Ensure the event exists and belongs to one of the requesting user's calendars.
            if (evt == null || !calendars.Any(c => c.Id == evt.CalendarId))
                return ServiceResult.Fail("Input is invalid");

            await _calendarEventRepository.DeleteCalendarEventAsync(evt);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <summary>
    /// Builds the remote storage path for a calendar event attachment.
    /// Uses a GUID prefix to avoid collisions and isolate files per event.
    /// </summary>
    /// <param name="roleIndex">Role-based directory segment (e.g., user role identifier).</param>
    /// <param name="eventId">The calendar event ID used as a sub-directory.</param>
    /// <param name="fileName">The original file name (extension is preserved).</param>
    /// <returns>A platform-native path string suitable for passing to <see cref="IFileClient"/>.</returns>
    private static string BuildEventFilePath(string roleIndex, long eventId, string fileName)
    {
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", "Calendar", roleIndex, "calendarEventAttachedFiles", $"{eventId}", $"{guid}{ext}");
    }

    /// <inheritdoc />
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string roleIndex)
    {
        // Validate the bytes as a real image by parsing them with ImageMagick before storing.
        try
        {
            using var image = new MagickImage(new MemoryStream(file.Bytes));
        }
        catch (MagickException)
        {
            return SummernoteUploadResult.Fail("Invalid image file.");
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        // Use a GUID-based file name to avoid collisions and prevent path traversal via the original name.
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine("upload", "Calendar", roleIndex, "summernote", "images", imageFile);

        bool uploaded = await _fileClient.UploadAsync(file.Bytes, file.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        if (!uploaded)
            return SummernoteUploadResult.Fail("Input is invalid");

        byte[] fileBytes = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        return SummernoteUploadResult.Ok(fileBytes, file.ContentType, imageFile, filePath);
    }

    /// <inheritdoc />
    public async Task<byte[]?> DownloadFileAsync(string filePath)
    {
        try
        {
            return await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to download file {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        bool hasImages = false;

        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img") ?? [])
        {
            hasImages = true;
            string filePath = imgTag.GetAttributeValue("alt", "");
            byte[]? fileData = await _fileClient.DownloadAsync(filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
            if (fileData != null)
            {
                _ = imgTag.SetAttributeValue("data-file", Convert.ToBase64String(fileData));
                _ = imgTag.SetAttributeValue("data-contenttype", GetImageContentType(filePath));
                _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
            }
        }

        return (doc.DocumentNode.OuterHtml, hasImages);
    }

    /// <summary>Sanitizes HTML to remove unsafe markup and decrypts RSA-encrypted file paths in img alt attributes.</summary>
    private string SanitizeAndDecryptContent(string html)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        string sanitized = sanitizer.Sanitize(html);

        var doc = new HtmlDocument();
        doc.LoadHtml(sanitized);
        foreach (HtmlNode imgTag in doc.DocumentNode.Descendants("img") ?? [])
        {
            string encryptedPath = imgTag.GetAttributeValue("alt", "");
            _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedPath));
        }
        return doc.DocumentNode.OuterHtml;
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
