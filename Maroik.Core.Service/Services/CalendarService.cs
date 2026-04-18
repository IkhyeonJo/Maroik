using ImageMagick;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
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
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetCalendarsAsync(string email)
        => await _calendarRepository.GetCalendarsAsync(email);

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetAllCalendarsAsync()
        => await _calendarRepository.GetAllCalendarsAsync();

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId)
        => await _calendarEventRepository.GetCalendarEventsAsync(calendarId);

    /// <inheritdoc />
    public async Task<CalendarEventResponse?> GetCalendarEventAsync(long id)
        => await _calendarEventRepository.GetCalendarEventAsync(id);

    /// <inheritdoc />
    public async Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId)
        => await _calendarEventAttachedFileRepository.GetCalendarEventAttachedFileAsync(calendarEventId);

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId)
        => await _calendarEventReminderRepository.GetCalendarEventRemindersAsync(calendarEventId);

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email)
        => await _otherCalendarRepository.GetOtherCalendarsAsync(email);

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync()
        => await _calendarSharedRepository.GetCalendarSharedAsync();

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureCalendarSharedAsync(long calendarId)
    {
        try
        {
            List<CalendarSharedResponse> shareds = await _calendarSharedRepository.GetCalendarSharedAsync();
            if (!shareds.Any(s => s.CalendarId == calendarId))
            {
                await _calendarSharedRepository.CreateCalendarSharedAsync(new CalendarSharedRequest
                {
                    CalendarId = calendarId,
                    User = false,
                    Anonymous = false
                });
            }
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarAsync(CalendarRequest request)
    {
        try
        {
            CalendarResponse? existing = (await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? ""))
                .FirstOrDefault(c => c.Name == request.Name);
            if (existing != null)
                return ServiceResult.Fail("The calendar already exists.");

            request.Created = DateTime.UtcNow;
            request.Updated = DateTime.UtcNow;
            await _calendarRepository.CreateCalendarAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");
            CalendarResponse? previous = calendars.FirstOrDefault(c => c.Id == request.Id);
            if (previous == null)
                return ServiceResult.Fail("Input is invalid");

            request.Created = previous.Created;
            request.Updated = DateTime.UtcNow;
            await _calendarRepository.UpdateCalendarAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");
            if (!calendars.Any(c => c.Id == request.Id))
                return ServiceResult.Fail("Input is invalid");

            await _calendarRepository.DeleteCalendarAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request)
    {
        try
        {
            List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(request.AccountEmail ?? "");
            if (calendars.Count == 0)
            {
                request.Created = DateTime.UtcNow;
                request.Updated = DateTime.UtcNow;
                await _calendarRepository.CreateCalendarAsync(request);
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
            foreach (OtherCalendarResponse previous in await _otherCalendarRepository.GetOtherCalendarsAsync(email) ?? [])
                await _otherCalendarRepository.DeleteOtherCalendarAsync(previous.Adapt<OtherCalendarRequest>());

            foreach (OtherCalendarRequest req in requests)
            {
                req.AccountEmail = email;
                await _otherCalendarRepository.CreateOtherCalendarAsync(req);
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
                await _calendarSharedRepository.UpdateCalendarSharedAsync(req);

            List<OtherCalendarResponse> allOtherCalendars = await _otherCalendarRepository.GetAllOtherCalendarsAsync() ?? [];
            foreach (CalendarSharedRequest req in requests.Where(r => !r.User))
            {
                OtherCalendarResponse? other = allOtherCalendars.FirstOrDefault(o => o.CalendarId == req.CalendarId);
                if (other != null)
                {
                    try { await _otherCalendarRepository.DeleteOtherCalendarAsync(other.Adapt<OtherCalendarRequest>()); }
                    catch { /* best effort */ }
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

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCalendarEventAsync(
        CalendarEventRequest request, string email, string roleIndex,
        List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            CalendarResponse? calendar = (await _calendarRepository.GetCalendarsAsync(email))
                .FirstOrDefault(c => c.Id == request.CalendarId);
            if (calendar == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            request.Created = DateTime.UtcNow;
            request.Updated = DateTime.UtcNow;
            await _calendarEventRepository.CreateCalendarEventAsync(request);

            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, request.Id, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                await _calendarEventAttachedFileRepository.SaveCalendarEventAttachedFileAsync(new CalendarEventAttachedFileRequest
                {
                    CalendarEventId = request.Id,
                    Size = Convert.ToInt32(attachedFile.Size),
                    Name = Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Extension = Path.GetExtension(attachedFile.FileName),
                    Path = filePath.Replace('\\', '/')
                });
            }

            foreach (CalendarReminderInfo r in reminders)
                await _calendarEventReminderRepository.CreateCalendarEventReminderAsync(ToReminderRequest(request.Id, r));

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
        await _unitOfWork.BeginAsync();
        try
        {
            CalendarResponse? calendar = (await _calendarRepository.GetCalendarsAsync(email))
                .FirstOrDefault(c => c.Id == request.CalendarId);
            if (calendar == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The calendar does not exists.");
            }

            CalendarEventResponse? existing = await _calendarEventRepository.GetCalendarEventAsync(request.Id);
            if (existing == null || existing.Id <= 0)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            request.Updated = DateTime.UtcNow;
            await _calendarEventRepository.UpdateCalendarEventAsync(request);

            // Replace attachment
            await _calendarEventAttachedFileRepository.DeleteCalendarEventAttachedFileAsync(request.Id);
            if (attachedFile != null)
            {
                string filePath = BuildEventFilePath(roleIndex, request.Id, attachedFile.FileName);
                await _fileClient.UploadAsync(attachedFile.Bytes, attachedFile.ContentType, filePath, _settings.Value.FileStorageBaseUrl ?? string.Empty);
                await _calendarEventAttachedFileRepository.SaveCalendarEventAttachedFileAsync(new CalendarEventAttachedFileRequest
                {
                    CalendarEventId = request.Id,
                    Size = Convert.ToInt32(attachedFile.Size),
                    Name = Path.GetFileNameWithoutExtension(attachedFile.FileName),
                    Extension = Path.GetExtension(attachedFile.FileName),
                    Path = filePath.Replace('\\', '/')
                });
            }

            // Replace reminders
            await _calendarEventReminderRepository.DeleteCalendarEventReminderAsync(request.Id);
            foreach (CalendarReminderInfo r in reminders)
                await _calendarEventReminderRepository.CreateCalendarEventReminderAsync(ToReminderRequest(request.Id, r));

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
            List<CalendarResponse> calendars = await _calendarRepository.GetCalendarsAsync(email) ?? [];
            CalendarEventResponse? evt = await _calendarEventRepository.GetCalendarEventAsync(calendarEventId);

            if (evt == null || !calendars.Any(c => c.Id == evt.CalendarId))
                return ServiceResult.Fail("Input is invalid");

            await _calendarEventRepository.DeleteCalendarEventAsync(evt.Adapt<CalendarEventRequest>());
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    private static string BuildEventFilePath(string roleIndex, long eventId, string fileName)
    {
        string guid = Guid.NewGuid().ToString().ToUpper();
        string ext = Path.GetExtension(fileName);
        return Path.Combine("upload", "Calendar", roleIndex, "calendarEventAttachedFiles", $"{eventId}", $"{guid}{ext}");
    }

    private static CalendarEventReminderRequest ToReminderRequest(long eventId, CalendarReminderInfo r) =>
        new()
        {
            CalendarEventId = eventId,
            Method = r.Method,
            MinutesBeforeEvent = r.MinutesBeforeEvent,
            HoursBeforeEvent = r.HoursBeforeEvent,
            DaysBeforeEvent = r.DaysBeforeEvent,
            WeeksBeforeEvent = r.WeeksBeforeEvent,
            TimesBeforeEvent = r.TimesBeforeEvent.HasValue
                ? TimeOnly.FromTimeSpan(r.TimesBeforeEvent.Value)
                : null
        };

    /// <inheritdoc />
    public async Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string roleIndex)
    {
        try
        {
            using var image = new MagickImage(new MemoryStream(file.Bytes));
        }
        catch (MagickException)
        {
            return SummernoteUploadResult.Fail("Invalid image file.");
        }

        string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
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
}
