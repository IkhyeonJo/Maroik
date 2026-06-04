using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendar = Maroik.Core.PostgreSQL.Models.Calendar;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarRepository"/> using an EF Core InMemory database.
/// </summary>
public class CalendarRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfCalendar MakeCalendar(string email, string name, string color = "#FF5733") => new()
    {
        AccountEmail = email,
        Name = name,
        Description = "Test calendar",
        TimeZoneIanaId = "UTC",
        HtmlColorCode = color,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params EfCalendar[] calendars)
    {
        await _context.Calendars.AddRangeAsync(calendars);
        await _context.SaveChangesAsync();
    }

    // -- GetAllCalendarsAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetAllCalendarsAsync</c> returns all calendars.</summary>
    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsAllCalendars()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("bob@example.com", "Personal"));

        List<Calendar> result = await _sut.GetAllOrderedByNameAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
    }

    /// <summary>Verifies that <c>GetAllCalendarsAsync</c> returns empty when no calendars.</summary>
    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsEmpty_WhenNoCalendars()
    {
        List<Calendar> result = await _sut.GetAllOrderedByNameAsync(TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Verifies that <c>GetAllCalendarsAsync</c> returns ordered by name.</summary>
    [Fact]
    public async Task GetAllCalendarsAsync_ReturnsOrderedByName()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("alice@example.com", "Personal"));

        List<Calendar> result = await _sut.GetAllOrderedByNameAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Personal", result[0].Name);
        Assert.Equal("Work", result[1].Name);
    }

    // -- GetCalendarsAsync ----------------------------------------------------

    /// <summary>Verifies that <c>GetCalendarsAsync</c> returns only calendars for given email.</summary>
    [Fact]
    public async Task GetCalendarsAsync_ReturnsOnlyCalendarsForGivenEmail()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Work"),
            MakeCalendar("alice@example.com", "Personal"),
            MakeCalendar("bob@example.com", "Bob Calendar"));

        List<Calendar> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("alice@example.com", c.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetCalendarsAsync</c> returns empty when no calendars for email.</summary>
    [Fact]
    public async Task GetCalendarsAsync_ReturnsEmpty_WhenNoCalendarsForEmail()
    {
        List<Calendar> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateCalendarAsync --------------------------------------------------

    /// <summary>Verifies that <c>CreateCalendarAsync</c> adds calendar to database.</summary>
    [Fact]
    public async Task CreateCalendarAsync_AddsCalendarToDatabase()
    {
        var calendar = Calendar.Reconstitute(
            id: 0,
            accountEmail: "alice@example.com",
            name: "New Calendar",
            description: "My new calendar",
            timeZoneIanaId: "Asia/Seoul",
            htmlColorCode: "#3498DB",
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(calendar, TestContext.Current.CancellationToken);

        EfCalendar? saved = await _context.Calendars.FirstOrDefaultAsync(
            c => c.AccountEmail == "alice@example.com" && c.Name == "New Calendar", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("#3498DB", saved.HtmlColorCode);
        Assert.Equal("Asia/Seoul", saved.TimeZoneIanaId);
    }

    // -- UpdateEntityAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing calendar.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingCalendar()
    {
        await SeedAsync(MakeCalendar("alice@example.com", "Work", color: "#FF5733"));
        EfCalendar efCalendar = await _context.Calendars.FirstAsync(TestContext.Current.CancellationToken);

        var calendar = Calendar.Reconstitute(
            id: efCalendar.Id,
            accountEmail: "alice@example.com",
            name: "Work Updated",
            description: "Updated description",
            timeZoneIanaId: "Asia/Seoul",
            htmlColorCode: "#2ECC71",
            created: efCalendar.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(calendar, TestContext.Current.CancellationToken);

        EfCalendar? updated = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == efCalendar.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Work Updated", updated.Name);
        Assert.Equal("#2ECC71", updated.HtmlColorCode);
    }

    // -- DeleteCalendarAsync --------------------------------------------------

    /// <summary>Verifies that <c>DeleteCalendarAsync</c> removes calendar from database.</summary>
    [Fact]
    public async Task DeleteCalendarAsync_RemovesCalendarFromDatabase()
    {
        await SeedAsync(MakeCalendar("alice@example.com", "ToDelete"));
        EfCalendar efCalendar = await _context.Calendars.FirstAsync(TestContext.Current.CancellationToken);

        var calendar = Calendar.Reconstitute(
            id: efCalendar.Id,
            accountEmail: "alice@example.com",
            name: "ToDelete",
            description: null,
            timeZoneIanaId: "UTC",
            htmlColorCode: "#FF5733",
            created: efCalendar.Created,
            updated: efCalendar.Updated);

        await _sut.DeleteByIdAsync(calendar.Id, TestContext.Current.CancellationToken);

        EfCalendar? deleted = await _context.Calendars.FirstOrDefaultAsync(c => c.Id == efCalendar.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteCalendarAsync</c> does not affect other calendars.</summary>
    [Fact]
    public async Task DeleteCalendarAsync_DoesNotAffectOtherCalendars()
    {
        await SeedAsync(
            MakeCalendar("alice@example.com", "Cal1"),
            MakeCalendar("alice@example.com", "Cal2"));
        List<EfCalendar> all = await _context.Calendars.ToListAsync(TestContext.Current.CancellationToken);

        var calendar = Calendar.Reconstitute(
            id: all[0].Id,
            accountEmail: all[0].AccountEmail ?? "",
            name: all[0].Name ?? "",
            description: all[0].Description,
            timeZoneIanaId: all[0].TimeZoneIanaId ?? "UTC",
            htmlColorCode: all[0].HtmlColorCode ?? "#000000",
            created: all[0].Created,
            updated: all[0].Updated);

        await _sut.DeleteByIdAsync(calendar.Id, TestContext.Current.CancellationToken);

        List<EfCalendar> remaining = await _context.Calendars.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(all[1].Id, remaining[0].Id);
    }
}
