using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfOtherCalendar = Maroik.Core.PostgreSQL.Models.OtherCalendar;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="OtherCalendarRepository"/> using an EF Core InMemory database.
/// OtherCalendar uses a composite PK (AccountEmail, CalendarId).
/// FK constraints are not enforced by InMemory, so Account/Calendar parent records are omitted.
/// </summary>
public class OtherCalendarRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OtherCalendarRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public OtherCalendarRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new OtherCalendarRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfOtherCalendar MakeOtherCalendar(string email, long calendarId) => new()
    {
        AccountEmail = email,
        CalendarId = calendarId
    };

    private async Task SeedAsync(params EfOtherCalendar[] entries)
    {
        await _context.OtherCalendars.AddRangeAsync(entries);
        await _context.SaveChangesAsync();
    }

    // -- GetAllOtherCalendarsAsync --------------------------------------------

    /// <summary>Verifies that <c>GetAllOtherCalendarsAsync</c> returns all entries.</summary>
    [Fact]
    public async Task GetAllOtherCalendarsAsync_ReturnsAll()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 3),
            MakeOtherCalendar("bob@example.com", 1),
            MakeOtherCalendar("alice@example.com", 2));

        List<OtherCalendar> result = await _sut.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    /// <summary>Verifies that <c>GetAllOtherCalendarsAsync</c> returns empty when no entries.</summary>
    [Fact]
    public async Task GetAllOtherCalendarsAsync_ReturnsEmpty_WhenNoEntries()
    {
        List<OtherCalendar> result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    // -- GetOtherCalendarsAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetOtherCalendarsAsync</c> returns only calendars for given email.</summary>
    [Fact]
    public async Task GetOtherCalendarsAsync_ReturnsOnlyCalendarsForGivenEmail()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 1),
            MakeOtherCalendar("alice@example.com", 2),
            MakeOtherCalendar("bob@example.com", 3));

        List<OtherCalendar> result = await _sut.GetByAccountEmailAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("alice@example.com", r.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetOtherCalendarsAsync</c> returns empty when email not found.</summary>
    [Fact]
    public async Task GetOtherCalendarsAsync_ReturnsEmpty_WhenEmailNotFound()
    {
        List<OtherCalendar> result = await _sut.GetByAccountEmailAsync("ghost@example.com");

        Assert.Empty(result);
    }

    // -- CreateOtherCalendarAsync ---------------------------------------------

    /// <summary>Verifies that <c>CreateOtherCalendarAsync</c> persists entry.</summary>
    [Fact]
    public async Task CreateOtherCalendarAsync_PersistsEntry()
    {
        var otherCalendar = OtherCalendar.Reconstitute(
            accountEmail: "carol@example.com",
            calendarId: 10);

        await _sut.CreateAsync(otherCalendar);

        bool exists = await _context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == "carol@example.com" && oc.CalendarId == 10, TestContext.Current.CancellationToken);
        Assert.True(exists);
    }

    // -- DeleteOtherCalendarAsync ---------------------------------------------

    /// <summary>Verifies that <c>DeleteOtherCalendarAsync</c> removes entry by email and calendar id.</summary>
    [Fact]
    public async Task DeleteOtherCalendarAsync_RemovesEntry()
    {
        await SeedAsync(MakeOtherCalendar("dave@example.com", 7));

        await _sut.DeleteByAccountEmailAndCalendarIdAsync("dave@example.com", 7);

        bool exists = await _context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == "dave@example.com" && oc.CalendarId == 7, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    // -- DeleteByAccountEmailAsync --------------------------------------------

    /// <summary>Verifies that <c>DeleteByAccountEmailAsync</c> removes all entries for given email.</summary>
    [Fact]
    public async Task DeleteByAccountEmailAsync_RemovesAllEntriesForEmail()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 1),
            MakeOtherCalendar("alice@example.com", 2),
            MakeOtherCalendar("bob@example.com", 3));

        await _sut.DeleteByAccountEmailAsync("alice@example.com");

        Assert.False(await _context.OtherCalendars.AnyAsync(oc => oc.AccountEmail == "alice@example.com", TestContext.Current.CancellationToken));
        Assert.True(await _context.OtherCalendars.AnyAsync(oc => oc.AccountEmail == "bob@example.com", TestContext.Current.CancellationToken));
    }
}
