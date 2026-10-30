using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.Calendars;
using Maroik.Website.Tests.Fixtures;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="CalendarEventViewModelExtensions"/>.
/// <see cref="ICalendarService"/> is replaced with a Moq mock; the actual event-to-view-model
/// mapping is delegated to the real <c>CalendarViewModelMapper.ToDisplayViewModel</c>, which is
/// covered separately by <c>CalendarViewModelMapperTests</c>.
/// </summary>
public class CalendarEventViewModelExtensionsTests
{
    /// <summary>Fake <c>ICalendarService</c> injected into the system under test.</summary>
    private readonly FakeCalendarService _calendarService = new();

    /// <summary>A calendar with id <paramref name="id"/> and the given color.</summary>
    private static CalendarResponse MakeCalendar(long id, string? htmlColorCode = "#3788d8") => new()
    {
        Id = id,
        Name = $"Calendar{id}",
        HtmlColorCode = htmlColorCode
    };

    /// <summary>An all-day event titled <paramref name="title"/> in calendar <paramref name="calendarId"/>.</summary>
    private static CalendarEventResponse MakeEvent(long id, long calendarId, string title) => new()
    {
        Id = id,
        CalendarId = calendarId,
        Title = title,
        AllDay = true,
        StartDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc)
    };

    /// <summary>Makes the calendar service return <paramref name="events"/>, grouped by calendar id.</summary>
    private void SetUpEvents(params CalendarEventResponse[] events) =>
        _calendarService.OnGetCalendarEventsAsync = (_, _) => Task.FromResult<IReadOnlyDictionary<long, List<CalendarEventResponse>>>(events
            .GroupBy(e => e.CalendarId)
            .ToDictionary(g => g.Key, g => g.ToList()));

    // -- GetCalendarEventViewModelsAsync ---------------------------------------

    /// <summary>No calendars returns empty list.</summary>
    [Fact]
    public async Task NoCalendars_ReturnsEmptyList()
    {
        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Single calendar returns its events as view models.</summary>
    [Fact]
    public async Task SingleCalendar_ReturnsItsEventsAsViewModels()
    {
        var calendar = MakeCalendar(1);
        SetUpEvents(MakeEvent(100, 1, "Trip"), MakeEvent(101, 1, "Meeting"));

        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [calendar], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("Trip", result[0].Title);
        Assert.Equal("Meeting", result[1].Title);
    }

    /// <summary>Multiple calendars fetches events for all calendars in a single batch call and concatenates in calendar order.</summary>
    [Fact]
    public async Task MultipleCalendars_FetchesEventsInSingleBatchCallAndConcatenatesInCalendarOrder()
    {
        var calendar1 = MakeCalendar(1);
        var calendar2 = MakeCalendar(2);
        SetUpEvents(MakeEvent(100, 1, "FromCal1"), MakeEvent(200, 2, "FromCal2"));

        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [calendar1, calendar2], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("FromCal1", result[0].Title);
        Assert.Equal("FromCal2", result[1].Title);
        Assert.Single(_calendarService.GetCalendarEventsAsyncCalls);
    }

    /// <summary>Passes each calendars HTML color code to its own events.</summary>
    [Fact]
    public async Task PassesEachCalendarsHtmlColorCodeToItsOwnEvents()
    {
        var calendar1 = MakeCalendar(1, "#111111");
        var calendar2 = MakeCalendar(2, "#222222");
        SetUpEvents(MakeEvent(100, 1, "FromCal1"), MakeEvent(200, 2, "FromCal2"));

        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [calendar1, calendar2], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal("#111111", result[0].HtmlColorCode);
        Assert.Equal("#222222", result[1].HtmlColorCode);
    }

    /// <summary>Passes calendar type through to each view model.</summary>
    [Fact]
    public async Task PassesCalendarTypeThroughToEachViewModel()
    {
        var calendar = MakeCalendar(1);
        SetUpEvents(MakeEvent(100, 1, "Trip"));

        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [calendar], "UTC", "Other", TestContext.Current.CancellationToken);

        Assert.Equal("Other", result[0].CalendarType);
    }

    /// <summary>Applies user time zone conversion to each event.</summary>
    [Fact]
    public async Task AppliesUserTimeZoneConversionToEachEvent()
    {
        var calendar = MakeCalendar(1);
        SetUpEvents(MakeEvent(100, 1, "Trip"));

        var result = await _calendarService.GetCalendarEventViewModelsAsync(
            [calendar], "Asia/Seoul", null, TestContext.Current.CancellationToken);

        // All-day event: DisplayStartDate is derived from StartDate converted to the viewer's time zone.
        Assert.Equal("2025-06-10", result[0].DisplayStartDate);
        Assert.Equal("Asia/Seoul", result[0].StartDateTimeZoneIanaId);
    }

    /// <summary>Passes cancellation token through to the service.</summary>
    [Fact]
    public async Task PassesCancellationTokenThroughToTheService()
    {
        var calendar = MakeCalendar(1);
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        await _calendarService.GetCalendarEventViewModelsAsync([calendar], "UTC", null, cancellationToken);

        Assert.Equal(cancellationToken, Assert.Single(_calendarService.GetCalendarEventsAsyncCalls).Ct);
    }

    // -- GetCalendarEventDetailViewModelAsync ----------------------------------

    /// <summary>Makes the single-event lookup return <paramref name="evt"/> and the HTML preparation pass content through.</summary>
    private void SetUpDetailEvent(CalendarEventResponse? evt)
    {
        _calendarService.OnGetCalendarEventAsync = (_, _) => Task.FromResult(evt);
        _calendarService.OnPrepareHtmlForDisplayAsync = (_, _) => Task.FromResult(("<p>desc</p>", false));
        _calendarService.OnGetCalendarEventAttachedFileAsync = (_, _) => Task.FromResult<CalendarEventAttachedFileDto?>(null);
        _calendarService.OnGetCalendarEventRemindersAsync = (_, _) => Task.FromResult<IEnumerable<CalendarEventReminderDto>>([]);
    }

    /// <summary>Returns null when the event doesn't exist.</summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_ReturnsNull_WhenEventNotFound()
    {
        SetUpDetailEvent(null);

        var result = await _calendarService.GetCalendarEventDetailViewModelAsync(
            [MakeCalendar(1)], "UTC", 999, true, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Returns null when the event's calendar isn't in the given calendars.</summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_ReturnsNull_WhenEventDoesNotBelongToGivenCalendars()
    {
        SetUpDetailEvent(MakeEvent(100, calendarId: 2, "Trip"));

        var result = await _calendarService.GetCalendarEventDetailViewModelAsync(
            [MakeCalendar(1)], "UTC", 100, true, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Returns a populated view model when the event belongs to one of the given calendars.</summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_ReturnsPopulatedViewModel_WhenEventBelongsToGivenCalendar()
    {
        SetUpDetailEvent(MakeEvent(100, calendarId: 1, "Trip"));

        var result = await _calendarService.GetCalendarEventDetailViewModelAsync(
            [MakeCalendar(1)], "UTC", 100, true, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Trip", result.Title);
        Assert.Equal("#3788d8", result.HtmlColorCode);
        Assert.Equal("<p>desc</p>", result.Description);
    }

    /// <summary>
    /// The detail describes the attachment by name, extension and size only — its storage path stays on the server, and the file
    /// itself is not part of the detail (it is streamed by the download action).
    /// </summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_DescribesTheAttachment()
    {
        SetUpDetailEvent(MakeEvent(100, calendarId: 1, "Trip"));
        var attachment = new CalendarEventAttachedFileDto { Id = 1, CalendarEventId = 100, Name = "report", Extension = ".zip", Size = 3, Path = "attachments/report.zip" };
        _calendarService.OnGetCalendarEventAttachedFileAsync = (id, _) => Task.FromResult(id == 100 ? attachment : null);

        var result = await _calendarService.GetCalendarEventDetailViewModelAsync(
            [MakeCalendar(1)], "UTC", 100, true, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        CalendarEventAttachedFileOutputViewModel file = Assert.IsType<CalendarEventAttachedFileOutputViewModel>(result.CalendarEventAttachedFile);
        Assert.Equal("report", file.Name);
        Assert.Equal(".zip", file.Extension);
        Assert.Equal("0", file.SizeLabel);
    }

    /// <summary>
    /// The owner's detail carries the event's reminders; someone else's (a shared or subscribed calendar, or a visitor) carries an
    /// empty list without reading them: a reminder is an e-mail to the owner and nobody else's business.
    /// </summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_CarriesTheReminders_OnlyWhenAskedTo()
    {
        SetUpDetailEvent(MakeEvent(100, calendarId: 1, "Trip"));
        _calendarService.OnGetCalendarEventRemindersAsync = (id, _) => Task.FromResult<IEnumerable<CalendarEventReminderDto>>(
            id == 100 ? [new CalendarEventReminderDto { Id = 1, CalendarEventId = 100, MinutesBeforeEvent = 15 }] : []);

        var owners = await _calendarService.GetCalendarEventDetailViewModelAsync([MakeCalendar(1)], "UTC", 100, true, TestContext.Current.CancellationToken);
        var others = await _calendarService.GetCalendarEventDetailViewModelAsync([MakeCalendar(1)], "UTC", 100, false, TestContext.Current.CancellationToken);

        Assert.Contains("\"MinutesBeforeEvent\":15", owners!.SerializedCalendarReminders, StringComparison.Ordinal);
        Assert.Equal("[]", others!.SerializedCalendarReminders);
        Assert.Equal(100, Assert.Single(_calendarService.GetCalendarEventRemindersAsyncCalls).CalendarEventId);
    }

    /// <summary>Passes cancellation token through to the service.</summary>
    [Fact]
    public async Task GetCalendarEventDetailViewModelAsync_PassesCancellationTokenThroughToTheService()
    {
        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        _calendarService.OnGetCalendarEventAsync = (_, _) => Task.FromResult<CalendarEventResponse?>(MakeEvent(100, calendarId: 1, "Trip"));
        _calendarService.OnPrepareHtmlForDisplayAsync = (_, _) => Task.FromResult(("", false));

        await _calendarService.GetCalendarEventDetailViewModelAsync([MakeCalendar(1)], "UTC", 100, true, cancellationToken);

        Assert.Equal((100, cancellationToken), Assert.Single(_calendarService.GetCalendarEventAsyncCalls));
        Assert.All(_calendarService.PrepareHtmlForDisplayAsyncCalls, call => Assert.Equal(cancellationToken, call.Ct));
        Assert.All(_calendarService.GetCalendarEventAttachedFileAsyncCalls, call => Assert.Equal(cancellationToken, call.Ct));
        Assert.All(_calendarService.GetCalendarEventRemindersAsyncCalls, call => Assert.Equal(cancellationToken, call.Ct));
    }
}
