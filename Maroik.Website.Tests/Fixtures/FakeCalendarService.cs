using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ICalendarService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeCalendarService : ICalendarService
{
    /// <summary>What <c>GetCalendarsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<CalendarResponse>>>? OnGetCalendarsAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> GetCalendarsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<CalendarResponse>> GetCalendarsAsync(string email, CancellationToken ct)
    {
        lock (GetCalendarsAsyncCalls) GetCalendarsAsyncCalls.Add((email, ct));
        if (OnGetCalendarsAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.FromResult<List<CalendarResponse>>([]);
    }

    /// <summary>What <c>GetCalendarEventsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<IReadOnlyDictionary<long, List<CalendarEventResponse>>>>? OnGetCalendarEventsAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarEventsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> CalendarIds, CancellationToken Ct)> GetCalendarEventsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<long, List<CalendarEventResponse>>> GetCalendarEventsAsync(IEnumerable<long> calendarIds, CancellationToken ct)
    {
        lock (GetCalendarEventsAsyncCalls) GetCalendarEventsAsyncCalls.Add((calendarIds, ct));
        if (OnGetCalendarEventsAsync is { } on)
        {
            return on(calendarIds, ct);
        }

        return Task.FromResult<IReadOnlyDictionary<long, List<CalendarEventResponse>>>(new Dictionary<long, List<CalendarEventResponse>>());
    }

    /// <summary>What <c>GetCalendarEventAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<CalendarEventResponse?>>? OnGetCalendarEventAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarEventAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> GetCalendarEventAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<CalendarEventResponse?> GetCalendarEventAsync(long id, CancellationToken ct)
    {
        lock (GetCalendarEventAsyncCalls) GetCalendarEventAsyncCalls.Add((id, ct));
        if (OnGetCalendarEventAsync is { } on)
        {
            return on(id, ct);
        }

        return Task.FromResult<CalendarEventResponse?>(default);
    }

    /// <summary>What <c>GetCalendarEventAttachedFileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<CalendarEventAttachedFileDto?>>? OnGetCalendarEventAttachedFileAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarEventAttachedFileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, CancellationToken Ct)> GetCalendarEventAttachedFileAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<CalendarEventAttachedFileDto?> GetCalendarEventAttachedFileAsync(long calendarEventId, CancellationToken ct)
    {
        lock (GetCalendarEventAttachedFileAsyncCalls) GetCalendarEventAttachedFileAsyncCalls.Add((calendarEventId, ct));
        if (OnGetCalendarEventAttachedFileAsync is { } on)
        {
            return on(calendarEventId, ct);
        }

        return Task.FromResult<CalendarEventAttachedFileDto?>(default);
    }

    /// <summary>What <c>GetCalendarEventRemindersAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<IEnumerable<CalendarEventReminderDto>>>? OnGetCalendarEventRemindersAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarEventRemindersAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, CancellationToken Ct)> GetCalendarEventRemindersAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IEnumerable<CalendarEventReminderDto>> GetCalendarEventRemindersAsync(long calendarEventId, CancellationToken ct)
    {
        lock (GetCalendarEventRemindersAsyncCalls) GetCalendarEventRemindersAsyncCalls.Add((calendarEventId, ct));
        if (OnGetCalendarEventRemindersAsync is { } on)
        {
            return on(calendarEventId, ct);
        }

        return Task.FromResult<IEnumerable<CalendarEventReminderDto>>([]);
    }

    /// <summary>What <c>GetOtherCalendarsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<OtherCalendarResponse>>>? OnGetOtherCalendarsAsync { get; set; }

    /// <summary>The arguments of every <c>GetOtherCalendarsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> GetOtherCalendarsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email, CancellationToken ct)
    {
        lock (GetOtherCalendarsAsyncCalls) GetOtherCalendarsAsyncCalls.Add((email, ct));
        if (OnGetOtherCalendarsAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.FromResult<List<OtherCalendarResponse>>([]);
    }

    /// <summary>What <c>GetVisibleOtherCalendarsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AccountResponse?, CancellationToken, Task<List<CalendarResponse>>>? OnGetVisibleOtherCalendarsAsync { get; set; }

    /// <summary>The arguments of every <c>GetVisibleOtherCalendarsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(AccountResponse? LoggedInAccount, CancellationToken Ct)> GetVisibleOtherCalendarsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<CalendarResponse>> GetVisibleOtherCalendarsAsync(AccountResponse? loggedInAccount, CancellationToken ct)
    {
        lock (GetVisibleOtherCalendarsAsyncCalls) GetVisibleOtherCalendarsAsyncCalls.Add((loggedInAccount, ct));
        if (OnGetVisibleOtherCalendarsAsync is { } on)
        {
            return on(loggedInAccount, ct);
        }

        return Task.FromResult<List<CalendarResponse>>([]);
    }

    /// <summary>What <c>EnsureCalendarSharedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<ServiceResult>>? OnEnsureCalendarSharedAsync { get; set; }

    /// <summary>The arguments of every <c>EnsureCalendarSharedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> CalendarIds, CancellationToken Ct)> EnsureCalendarSharedAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> EnsureCalendarSharedAsync(IEnumerable<long> calendarIds, CancellationToken ct)
    {
        lock (EnsureCalendarSharedAsyncCalls) EnsureCalendarSharedAsyncCalls.Add((calendarIds, ct));
        if (OnEnsureCalendarSharedAsync is { } on)
        {
            return on(calendarIds, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>GetCalendarSharedSummariesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<CalendarSharedSummaryResponse>>>? OnGetCalendarSharedSummariesAsync { get; set; }

    /// <summary>The arguments of every <c>GetCalendarSharedSummariesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetCalendarSharedSummariesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<CalendarSharedSummaryResponse>> GetCalendarSharedSummariesAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetCalendarSharedSummariesAsyncCalls) GetCalendarSharedSummariesAsyncCalls.Add((accountEmail, ct));
        if (OnGetCalendarSharedSummariesAsync is { } on)
        {
            return on(accountEmail, ct);
        }

        return Task.FromResult<List<CalendarSharedSummaryResponse>>([]);
    }

    /// <summary>What <c>GetBrowseCalendarsOfInterestAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<CalendarBrowseSummaryResponse>>>? OnGetBrowseCalendarsOfInterestAsync { get; set; }

    /// <summary>The arguments of every <c>GetBrowseCalendarsOfInterestAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetBrowseCalendarsOfInterestAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<CalendarBrowseSummaryResponse>> GetBrowseCalendarsOfInterestAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetBrowseCalendarsOfInterestAsyncCalls) GetBrowseCalendarsOfInterestAsyncCalls.Add((accountEmail, ct));
        if (OnGetBrowseCalendarsOfInterestAsync is { } on)
        {
            return on(accountEmail, ct);
        }

        return Task.FromResult<List<CalendarBrowseSummaryResponse>>([]);
    }

    /// <summary>What <c>CreateCalendarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CalendarRequest, CancellationToken, Task<ServiceResult>>? OnCreateCalendarAsync { get; set; }

    /// <summary>The arguments of every <c>CreateCalendarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CalendarRequest Request, CancellationToken Ct)> CreateCalendarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CreateCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct)
    {
        lock (CreateCalendarAsyncCalls) CreateCalendarAsyncCalls.Add((accountEmail, request, ct));
        if (OnCreateCalendarAsync is { } on)
        {
            return on(accountEmail, request, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateCalendarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CalendarRequest, CancellationToken, Task<ServiceResult>>? OnUpdateCalendarAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateCalendarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CalendarRequest Request, CancellationToken Ct)> UpdateCalendarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct)
    {
        lock (UpdateCalendarAsyncCalls) UpdateCalendarAsyncCalls.Add((accountEmail, request, ct));
        if (OnUpdateCalendarAsync is { } on)
        {
            return on(accountEmail, request, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteCalendarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CalendarRequest, CancellationToken, Task<ServiceResult>>? OnDeleteCalendarAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteCalendarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CalendarRequest Request, CancellationToken Ct)> DeleteCalendarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct)
    {
        lock (DeleteCalendarAsyncCalls) DeleteCalendarAsyncCalls.Add((accountEmail, request, ct));
        if (OnDeleteCalendarAsync is { } on)
        {
            return on(accountEmail, request, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateOtherCalendarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IEnumerable<OtherCalendarRequest>, CancellationToken, Task<ServiceResult>>? OnUpdateOtherCalendarAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateOtherCalendarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, IEnumerable<OtherCalendarRequest> Requests, CancellationToken Ct)> UpdateOtherCalendarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests, CancellationToken ct)
    {
        lock (UpdateOtherCalendarAsyncCalls) UpdateOtherCalendarAsyncCalls.Add((email, requests, ct));
        if (OnUpdateOtherCalendarAsync is { } on)
        {
            return on(email, requests, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateCalendarSharedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IEnumerable<CalendarSharedRequest>, CancellationToken, Task<ServiceResult>>? OnUpdateCalendarSharedAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateCalendarSharedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, IEnumerable<CalendarSharedRequest> Requests, CancellationToken Ct)> UpdateCalendarSharedAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateCalendarSharedAsync(string email, IEnumerable<CalendarSharedRequest> requests, CancellationToken ct)
    {
        lock (UpdateCalendarSharedAsyncCalls) UpdateCalendarSharedAsyncCalls.Add((email, requests, ct));
        if (OnUpdateCalendarSharedAsync is { } on)
        {
            return on(email, requests, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>EnsureDefaultCalendarAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CalendarRequest, CancellationToken, Task<ServiceResult>>? OnEnsureDefaultCalendarAsync { get; set; }

    /// <summary>The arguments of every <c>EnsureDefaultCalendarAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CalendarRequest Request, CancellationToken Ct)> EnsureDefaultCalendarAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> EnsureDefaultCalendarAsync(string accountEmail, CalendarRequest request, CancellationToken ct)
    {
        lock (EnsureDefaultCalendarAsyncCalls) EnsureDefaultCalendarAsyncCalls.Add((accountEmail, request, ct));
        if (OnEnsureDefaultCalendarAsync is { } on)
        {
            return on(accountEmail, request, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>CreateCalendarEventAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventRequest, string, string, List<CalendarReminderDto>, AttachedFileDto?, CancellationToken, Task<ServiceResult>>? OnCreateCalendarEventAsync { get; set; }

    /// <summary>The arguments of every <c>CreateCalendarEventAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventRequest Request, string Email, string RoleIndex, List<CalendarReminderDto> Reminders, AttachedFileDto? AttachedFile, CancellationToken Ct)> CreateCalendarEventAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CreateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderDto> reminders, AttachedFileDto? attachedFile, CancellationToken ct)
    {
        lock (CreateCalendarEventAsyncCalls) CreateCalendarEventAsyncCalls.Add((request, email, roleIndex, reminders, attachedFile, ct));
        if (OnCreateCalendarEventAsync is { } on)
        {
            return on(request, email, roleIndex, reminders, attachedFile, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateCalendarEventAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarEventRequest, string, string, List<CalendarReminderDto>, AttachedFileDto?, CancellationToken, Task<ServiceResult>>? OnUpdateCalendarEventAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateCalendarEventAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarEventRequest Request, string Email, string RoleIndex, List<CalendarReminderDto> Reminders, AttachedFileDto? AttachedFile, CancellationToken Ct)> UpdateCalendarEventAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderDto> reminders, AttachedFileDto? attachedFile, CancellationToken ct)
    {
        lock (UpdateCalendarEventAsyncCalls) UpdateCalendarEventAsyncCalls.Add((request, email, roleIndex, reminders, attachedFile, ct));
        if (OnUpdateCalendarEventAsync is { } on)
        {
            return on(request, email, roleIndex, reminders, attachedFile, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteCalendarEventAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, string, CancellationToken, Task<ServiceResult>>? OnDeleteCalendarEventAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteCalendarEventAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, string Email, CancellationToken Ct)> DeleteCalendarEventAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email, CancellationToken ct)
    {
        lock (DeleteCalendarEventAsyncCalls) DeleteCalendarEventAsyncCalls.Add((calendarEventId, email, ct));
        if (OnDeleteCalendarEventAsync is { } on)
        {
            return on(calendarEventId, email, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UploadSummernoteImageAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AttachedFileDto, string, string, CancellationToken, Task<SummernoteUploadResult>>? OnUploadSummernoteImageAsync { get; set; }

    /// <summary>The arguments of every <c>UploadSummernoteImageAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(AttachedFileDto File, string RoleIndex, string ActorEmail, CancellationToken Ct)> UploadSummernoteImageAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileDto file, string roleIndex, string actorEmail, CancellationToken ct)
    {
        lock (UploadSummernoteImageAsyncCalls) UploadSummernoteImageAsyncCalls.Add((file, roleIndex, actorEmail, ct));
        if (OnUploadSummernoteImageAsync is { } on)
        {
            return on(file, roleIndex, actorEmail, ct);
        }

        return Task.FromResult<SummernoteUploadResult>(default!);
    }

    /// <summary>What <c>OpenCalendarEventAttachedFileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, AccountResponse?, CancellationToken, Task<(ServiceResult, AttachmentDownload?)>>? OnOpenCalendarEventAttachedFileAsync { get; set; }

    /// <summary>The arguments of every <c>OpenCalendarEventAttachedFileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long CalendarEventId, AccountResponse? Viewer, CancellationToken Ct)> OpenCalendarEventAttachedFileAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, AttachmentDownload?)> OpenCalendarEventAttachedFileAsync(long calendarEventId, AccountResponse? viewer, CancellationToken ct)
    {
        lock (OpenCalendarEventAttachedFileAsyncCalls) OpenCalendarEventAttachedFileAsyncCalls.Add((calendarEventId, viewer, ct));
        if (OnOpenCalendarEventAttachedFileAsync is { } on)
        {
            return on(calendarEventId, viewer, ct);
        }

        return Task.FromResult<(ServiceResult, AttachmentDownload?)>(default);
    }

    /// <summary>What <c>PrepareHtmlForDisplayAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<(string, bool)>>? OnPrepareHtmlForDisplayAsync { get; set; }

    /// <summary>The arguments of every <c>PrepareHtmlForDisplayAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Html, CancellationToken Ct)> PrepareHtmlForDisplayAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(string, bool)> PrepareHtmlForDisplayAsync(string html, CancellationToken ct)
    {
        lock (PrepareHtmlForDisplayAsyncCalls) PrepareHtmlForDisplayAsyncCalls.Add((html, ct));
        if (OnPrepareHtmlForDisplayAsync is { } on)
        {
            return on(html, ct);
        }

        return Task.FromResult<(string, bool)>(default);
    }
}
