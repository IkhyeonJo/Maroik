using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarRepository"/> using an EF Core InMemory database.
/// </summary>
public class CalendarRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarRepository _sut;

    public CalendarRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Calendar MakeCalendar(string email, string name, string color = "#FF5733") => new()
    {
        AccountEmail = email,
        Name = name,
        Description = "Test calendar",
        TimeZoneIanaId = "UTC",
        HtmlColorCode = color,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params Calendar[] calendars)
    {
        await _context.Calendars.AddRangeAsync(calendars);
        await _context.SaveChangesAsync();
    }

    // ── GetAllCalendarsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsAllCalendars()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("bob@example.com", "Personal"));

        List<CalendarResponse> result = await _sut.GetAllCalendarsAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsEmpty_WhenNoCalendars()
    {
        List<CalendarResponse> result = await _sut.GetAllCalendarsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsOrderedByName()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("alice@example.com", "Personal"));

        List<CalendarResponse> result = await _sut.GetAllCalendarsAsync();

        Assert.Equal("Personal", result[0].Name);
        Assert.Equal("Work", result[1].Name);
    }

    // ── GetCalendarsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarsAsync_ReturnsOnlyCalendarsForGivenEmail()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("alice@example.com", "Personal"),
            MakeCalendar("bob@example.com", "Bob Calendar"));

        List<CalendarResponse> result = await _sut.GetCalendarsAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("alice@example.com", c.AccountEmail));
    }

    [Fact]
    public async Task GetCalendarsAsync_ReturnsEmpty_WhenNoCalendarsForEmail()
    {
        List<CalendarResponse> result = await _sut.GetCalendarsAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── CreateCalendarAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateCalendarAsync_AddsCalendarToDatabase()
    {
        var request = new CalendarRequest
        {
            AccountEmail = "alice@example.com",
            Name = "New Calendar",
            Description = "My new calendar",
            TimeZoneIanaId = "Asia/Seoul",
            HtmlColorCode = "#3498DB",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.CreateCalendarAsync(request);

        Calendar? saved = await _context.Calendars.FirstOrDefaultAsync(
            c => c.AccountEmail == "alice@example.com" && c.Name == "New Calendar", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("#3498DB", saved.HtmlColorCode);
        Assert.Equal("Asia/Seoul", saved.TimeZoneIanaId);
    }

    // ── UpdateCalendarAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendarAsync_UpdatesExistingCalendar()
    {
        await SeedAsync(MakeCalendar("alice@example.com", "Work", color: "#FF5733"));
        Calendar calendar = await _context.Calendars.FirstAsync(TestContext.Current.CancellationToken);

        var request = new CalendarRequest
        {
            Id = calendar.Id,
            AccountEmail = "alice@example.com",
            Name = "Work Updated",
            Description = "Updated description",
            TimeZoneIanaId = "Asia/Seoul",
            HtmlColorCode = "#2ECC71",
            Created = calendar.Created,
            Updated = DateTime.UtcNow
        };

        await _sut.UpdateCalendarAsync(request);

        Calendar? updated = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendar.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Work Updated", updated.Name);
        Assert.Equal("#2ECC71", updated.HtmlColorCode);
    }

    // ── DeleteCalendarAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCalendarAsync_RemovesCalendarFromDatabase()
    {
        await SeedAsync(MakeCalendar("alice@example.com", "ToDelete"));
        Calendar calendar = await _context.Calendars.FirstAsync(TestContext.Current.CancellationToken);

        var request = new CalendarRequest
        {
            Id = calendar.Id,
            AccountEmail = "alice@example.com",
            Name = "ToDelete",
            TimeZoneIanaId = "UTC",
            HtmlColorCode = "#FF5733",
            Created = calendar.Created,
            Updated = calendar.Updated
        };

        await _sut.DeleteCalendarAsync(request);

        Calendar? deleted = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == calendar.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteCalendarAsync_DoesNotAffectOtherCalendars()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Cal1"),
            MakeCalendar("alice@example.com", "Cal2"));
        List<Calendar> all = await _context.Calendars.ToListAsync(TestContext.Current.CancellationToken);

        var request = new CalendarRequest
        {
            Id = all[0].Id,
            AccountEmail = all[0].AccountEmail,
            Name = all[0].Name,
            TimeZoneIanaId = all[0].TimeZoneIanaId,
            HtmlColorCode = all[0].HtmlColorCode,
            Created = all[0].Created,
            Updated = all[0].Updated
        };

        await _sut.DeleteCalendarAsync(request);

        List<Calendar> remaining = await _context.Calendars.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
