using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendarEvent = Maroik.Core.PostgreSQL.Models.CalendarEvent;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventRepository"/> using an EF Core InMemory database.
/// FK constraints are not enforced by InMemory, so Calendar parent records are omitted.
/// </summary>
public class CalendarEventRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarEventRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfCalendarEvent MakeEvent(long calendarId, string title = "Meeting") => new()
    {
        CalendarId = calendarId,
        Title = title,
        AllDay = false,
        StartDate = DateTime.UtcNow,
        EndDate = DateTime.UtcNow.AddHours(1),
        Status = "Busy",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params EfCalendarEvent[] events)
    {
        await _context.CalendarEvents.AddRangeAsync(events);
        await _context.SaveChangesAsync();
    }

    // -- GetCalendarEventsAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetCalendarEventsAsync</c> returns only events for given calendar.</summary>
    [Fact]
    public async Task GetCalendarEventsAsync_ReturnsOnlyEventsForGivenCalendar()
    {
        await SeedAsync(
            MakeEvent(1, "Event A"),
            MakeEvent(1, "Event B"),
            MakeEvent(2, "Other Calendar Event"));

        IEnumerable<CalendarEvent> result = await _sut.GetByCalendarIdAsync(1L, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal(1, e.CalendarId));
    }

    /// <summary>Verifies that <c>GetCalendarEventsAsync</c> returns empty when no events for calendar.</summary>
    [Fact]
    public async Task GetCalendarEventsAsync_ReturnsEmpty_WhenNoEventsForCalendar()
    {
        IEnumerable<CalendarEvent> result = await _sut.GetByCalendarIdAsync(99L, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetByCalendarIdsAsync --------------------------------------------------

    /// <summary>Verifies that <c>GetByCalendarIdsAsync</c> returns events for every requested calendar in one query.</summary>
    [Fact]
    public async Task GetByCalendarIdsAsync_ReturnsEventsForAllRequestedCalendars()
    {
        await SeedAsync(
            MakeEvent(1, "Event A"),
            MakeEvent(2, "Event B"),
            MakeEvent(3, "Not Requested"));

        List<CalendarEvent> result = await _sut.GetByCalendarIdsAsync([1L, 2L], TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.CalendarId == 1);
        Assert.Contains(result, e => e.CalendarId == 2);
        Assert.DoesNotContain(result, e => e.CalendarId == 3);
    }

    /// <summary>Verifies that <c>GetByCalendarIdsAsync</c> returns empty when no calendar IDs match.</summary>
    [Fact]
    public async Task GetByCalendarIdsAsync_ReturnsEmpty_WhenNoCalendarIdsMatch()
    {
        await SeedAsync(MakeEvent(1, "Event A"));

        List<CalendarEvent> result = await _sut.GetByCalendarIdsAsync([98L, 99L], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Verifies that <c>GetByCalendarIdsAsync</c> returns empty when given an empty ID collection.</summary>
    [Fact]
    public async Task GetByCalendarIdsAsync_ReturnsEmpty_WhenCalendarIdsEmpty()
    {
        await SeedAsync(MakeEvent(1, "Event A"));

        List<CalendarEvent> result = await _sut.GetByCalendarIdsAsync([], TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetCalendarEventAsync ------------------------------------------------

    /// <summary>Verifies that <c>GetCalendarEventAsync</c> returns event when found.</summary>
    [Fact]
    public async Task GetCalendarEventAsync_ReturnsEvent_WhenFound()
    {
        await SeedAsync(MakeEvent(1, "Team Standup"));

        long id = (await _context.CalendarEvents.FirstAsync(TestContext.Current.CancellationToken)).Id;
        CalendarEvent? result = await _sut.FindByIdAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Team Standup", result.Title);
    }

    /// <summary>Verifies that <c>GetCalendarEventAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetCalendarEventAsync_ReturnsNull_WhenNotFound()
    {
        CalendarEvent? result = await _sut.FindByIdAsync(999L, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateCalendarEventAsync ---------------------------------------------

    /// <summary>Verifies that <c>CreateCalendarEventAsync</c> persists event.</summary>
    [Fact]
    public async Task CreateCalendarEventAsync_PersistsEvent()
    {
        var calendarEvent = CalendarEvent.Reconstitute(
            id: 0,
            calendarId: 5,
            title: "New Event",
            description: null,
            allDay: true,
            startDate: DateTime.UtcNow.Date,
            endDate: DateTime.UtcNow.Date.AddDays(1),
            startTz: null,
            endTz: null,
            location: null,
            status: "Busy",
            recurrenceId: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateCalendarEventAsync(calendarEvent, TestContext.Current.CancellationToken);

        EfCalendarEvent? saved = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.CalendarId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("New Event", saved.Title);
        Assert.True(saved.AllDay);
    }

    // -- UpdateEntityAsync ---------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing event.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingEvent()
    {
        await SeedAsync(MakeEvent(3, "Old Title"));

        EfCalendarEvent efEvent = await _context.CalendarEvents.FirstAsync(e => e.CalendarId == 3, TestContext.Current.CancellationToken);

        var calendarEvent = CalendarEvent.Reconstitute(
            id: efEvent.Id,
            calendarId: 3,
            title: "Updated Title",
            description: null,
            allDay: false,
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddHours(2),
            startTz: null,
            endTz: null,
            location: null,
            status: "Free",
            recurrenceId: null,
            created: efEvent.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(calendarEvent, TestContext.Current.CancellationToken);

        EfCalendarEvent? updated = await _context.CalendarEvents.FindAsync(new object[] { efEvent.Id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Free", updated.Status);
    }

    // -- DeleteCalendarEventAsync ---------------------------------------------

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> removes event.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_RemovesEvent()
    {
        await SeedAsync(MakeEvent(4, "To Be Deleted"));

        EfCalendarEvent efEvent = await _context.CalendarEvents.FirstAsync(e => e.CalendarId == 4, TestContext.Current.CancellationToken);

        var calendarEvent = CalendarEvent.Reconstitute(
            id: efEvent.Id,
            calendarId: efEvent.CalendarId,
            title: efEvent.Title ?? "",
            description: efEvent.Description,
            allDay: efEvent.AllDay,
            startDate: efEvent.StartDate,
            endDate: efEvent.EndDate,
            startTz: efEvent.StartDateTimeZoneIanaId,
            endTz: efEvent.EndDateTimeZoneIanaId,
            location: efEvent.Location,
            status: efEvent.Status,
            recurrenceId: efEvent.RecurrenceId,
            created: efEvent.Created,
            updated: efEvent.Updated);

        await _sut.DeleteByIdAsync(calendarEvent.Id, TestContext.Current.CancellationToken);

        bool exists = await _context.CalendarEvents.AnyAsync(e => e.Id == efEvent.Id, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }
}
