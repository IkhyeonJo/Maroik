using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Extensions;
using Moq;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="CalendarEventViewModelExtensions"/>.
/// <see cref="ICalendarService"/> is replaced with a Moq mock; the actual event-to-view-model
/// mapping is delegated to the real <c>CalendarViewModelMapper.ToDisplayViewModel</c>, which is
/// covered separately by <c>CalendarViewModelMapperTests</c>.
/// </summary>
public class CalendarEventViewModelExtensionsTests
{
    private readonly Mock<ICalendarService> _calendarService = new();

    private static CalendarResponse MakeCalendar(long id, string? htmlColorCode = "#3788d8") => new()
    {
        Id = id,
        Name = $"Calendar{id}",
        HtmlColorCode = htmlColorCode
    };

    private static CalendarEventResponse MakeEvent(long id, long calendarId, string title) => new()
    {
        Id = id,
        CalendarId = calendarId,
        Title = title,
        AllDay = true,
        StartDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
        EndDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc)
    };

    // -- GetCalendarEventViewModelsAsync ---------------------------------------

    /// <summary>No calendars returns empty list.</summary>
    [Fact]
    public async Task NoCalendars_ReturnsEmptyList()
    {
        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
            [], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Empty(result);
        _calendarService.Verify(
            s => s.GetCalendarEventsAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Single calendar returns its events as view models.</summary>
    [Fact]
    public async Task SingleCalendar_ReturnsItsEventsAsViewModels()
    {
        var calendar = MakeCalendar(1);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)
                [MakeEvent(100, 1, "Trip"), MakeEvent(101, 1, "Meeting")]);

        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
            [calendar], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("Trip", result[0].Title);
        Assert.Equal("Meeting", result[1].Title);
    }

    /// <summary>Multiple calendars fetches events for each and concatenates in calendar order.</summary>
    [Fact]
    public async Task MultipleCalendars_FetchesEventsForEachAndConcatenatesInCalendarOrder()
    {
        var calendar1 = MakeCalendar(1);
        var calendar2 = MakeCalendar(2);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(100, 1, "FromCal1")]);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(2L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(200, 2, "FromCal2")]);

        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
            [calendar1, calendar2], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal("FromCal1", result[0].Title);
        Assert.Equal("FromCal2", result[1].Title);
        _calendarService.Verify(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()), Times.Once);
        _calendarService.Verify(s => s.GetCalendarEventsAsync(2L, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Passes each calendars html color code to its own events.</summary>
    [Fact]
    public async Task PassesEachCalendarsHtmlColorCodeToItsOwnEvents()
    {
        var calendar1 = MakeCalendar(1, "#111111");
        var calendar2 = MakeCalendar(2, "#222222");
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(100, 1, "FromCal1")]);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(2L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(200, 2, "FromCal2")]);

        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
            [calendar1, calendar2], "UTC", null, TestContext.Current.CancellationToken);

        Assert.Equal("#111111", result[0].HtmlColorCode);
        Assert.Equal("#222222", result[1].HtmlColorCode);
    }

    /// <summary>Passes calendar type through to each view model.</summary>
    [Fact]
    public async Task PassesCalendarTypeThroughToEachViewModel()
    {
        var calendar = MakeCalendar(1);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(100, 1, "Trip")]);

        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
            [calendar], "UTC", "Other", TestContext.Current.CancellationToken);

        Assert.Equal("Other", result[0].CalendarType);
    }

    /// <summary>Applies user time zone conversion to each event.</summary>
    [Fact]
    public async Task AppliesUserTimeZoneConversionToEachEvent()
    {
        var calendar = MakeCalendar(1);
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[MakeEvent(100, 1, "Trip")]);

        var result = await _calendarService.Object.GetCalendarEventViewModelsAsync(
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
        _calendarService
            .Setup(s => s.GetCalendarEventsAsync(1L, cts.Token))
            .ReturnsAsync((IEnumerable<CalendarEventResponse>)[]);

        await _calendarService.Object.GetCalendarEventViewModelsAsync([calendar], "UTC", null, cts.Token);

        _calendarService.Verify(s => s.GetCalendarEventsAsync(1L, cts.Token), Times.Once);
    }
}
