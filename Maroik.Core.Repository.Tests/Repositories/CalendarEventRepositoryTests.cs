using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventRepository"/> using an EF Core InMemory database.
/// FK constraints are not enforced by InMemory, so Calendar parent records are omitted.
/// </summary>
public class CalendarEventRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventRepository _sut;

    public CalendarEventRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarEventRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CalendarEvent MakeEvent(long calendarId, string title = "Meeting") => new()
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

    private async Task SeedAsync(params CalendarEvent[] events)
    {
        await _context.CalendarEvents.AddRangeAsync(events);
        await _context.SaveChangesAsync();
    }

    // ── GetCalendarEventsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarEventsAsync_ReturnsOnlyEventsForGivenCalendar()
    {
        await SeedAsync(
            MakeEvent(1, "Event A"),
            MakeEvent(1, "Event B"),
            MakeEvent(2, "Other Calendar Event"));

        IEnumerable<CalendarEventResponse> result = await _sut.GetCalendarEventsAsync(1);

        Assert.Equal(2, result.Count());
        Assert.All(result, e => Assert.Equal(1, e.CalendarId));
    }

    [Fact]
    public async Task GetCalendarEventsAsync_ReturnsEmpty_WhenNoEventsForCalendar()
    {
        IEnumerable<CalendarEventResponse> result = await _sut.GetCalendarEventsAsync(99);

        Assert.Empty(result);
    }

    // ── GetCalendarEventAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarEventAsync_ReturnsEvent_WhenFound()
    {
        await SeedAsync(MakeEvent(1, "Team Standup"));

        long id = (await _context.CalendarEvents.FirstAsync(TestContext.Current.CancellationToken)).Id;
        CalendarEventResponse? result = await _sut.GetCalendarEventAsync(id);

        Assert.NotNull(result);
        Assert.Equal("Team Standup", result.Title);
    }

    [Fact]
    public async Task GetCalendarEventAsync_ReturnsNull_WhenNotFound()
    {
        CalendarEventResponse? result = await _sut.GetCalendarEventAsync(999);

        Assert.Null(result);
    }

    // ── CreateCalendarEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task CreateCalendarEventAsync_PersistsEvent()
    {
        var request = new CalendarEventRequest
        {
            CalendarId = 5,
            Title = "New Event",
            AllDay = true,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(1),
            Status = "Busy",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.CreateCalendarEventAsync(request);

        CalendarEvent? saved = await _context.CalendarEvents.FirstOrDefaultAsync(e => e.CalendarId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("New Event", saved.Title);
        Assert.True(saved.AllDay);
    }

    // ── UpdateCalendarEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendarEventAsync_UpdatesExistingEvent()
    {
        await SeedAsync(MakeEvent(3, "Old Title"));

        long id = (await _context.CalendarEvents.FirstAsync(e => e.CalendarId == 3, TestContext.Current.CancellationToken)).Id;

        var request = new CalendarEventRequest
        {
            Id = id,
            CalendarId = 3,
            Title = "Updated Title",
            AllDay = false,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddHours(2),
            Status = "Free",
            Created = DateTime.UtcNow.AddDays(-1),
            Updated = DateTime.UtcNow
        };

        await _sut.UpdateCalendarEventAsync(request);

        CalendarEvent? updated = await _context.CalendarEvents.FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Free", updated.Status);
    }

    // ── DeleteCalendarEventAsync ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteCalendarEventAsync_RemovesEvent()
    {
        await SeedAsync(MakeEvent(4, "To Be Deleted"));

        CalendarEvent existing = await _context.CalendarEvents.FirstAsync(e => e.CalendarId == 4, TestContext.Current.CancellationToken);

        var request = new CalendarEventRequest
        {
            Id = existing.Id,
            CalendarId = existing.CalendarId,
            Title = existing.Title,
            StartDate = existing.StartDate,
            EndDate = existing.EndDate,
            Status = existing.Status!,
            Created = existing.Created,
            Updated = existing.Updated
        };

        await _sut.DeleteCalendarEventAsync(request);

        bool exists = await _context.CalendarEvents.AnyAsync(e => e.Id == existing.Id, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }
}
