using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static OtherCalendar MakeOtherCalendar(string email, long calendarId) => new()
    {
        AccountEmail = email,
        CalendarId = calendarId
    };

    private async Task SeedAsync(params OtherCalendar[] entries)
    {
        await _context.OtherCalendars.AddRangeAsync(entries);
        await _context.SaveChangesAsync();
    }

    // ── GetAllOtherCalendarsAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>GetAllOtherCalendarsAsync</c> returns all when ordered by calendar id.</summary>
    [Fact]
    public async Task GetAllOtherCalendarsAsync_ReturnsAll_OrderedByCalendarId()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 3),
            MakeOtherCalendar("bob@example.com", 1),
            MakeOtherCalendar("alice@example.com", 2));

        List<OtherCalendarResponse> result = await _sut.GetAllOtherCalendarsAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].CalendarId);
        Assert.Equal(2, result[1].CalendarId);
        Assert.Equal(3, result[2].CalendarId);
    }

    /// <summary>Verifies that <c>GetAllOtherCalendarsAsync</c> returns empty when no entries.</summary>
    [Fact]
    public async Task GetAllOtherCalendarsAsync_ReturnsEmpty_WhenNoEntries()
    {
        List<OtherCalendarResponse> result = await _sut.GetAllOtherCalendarsAsync();

        Assert.Empty(result);
    }

    // ── GetOtherCalendarsAsync ───────────────────────────────────────────────

    /// <summary>Verifies that <c>GetOtherCalendarsAsync</c> returns only calendars for given email.</summary>
    [Fact]
    public async Task GetOtherCalendarsAsync_ReturnsOnlyCalendarsForGivenEmail()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 1),
            MakeOtherCalendar("alice@example.com", 2),
            MakeOtherCalendar("bob@example.com", 3));

        List<OtherCalendarResponse> result = await _sut.GetOtherCalendarsAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("alice@example.com", r.AccountEmail));
    }

    /// <summary>Verifies that <c>GetOtherCalendarsAsync</c> returns results when ordered by calendar id.</summary>
    [Fact]
    public async Task GetOtherCalendarsAsync_ReturnsResults_OrderedByCalendarId()
    {
        await SeedAsync(
            MakeOtherCalendar("alice@example.com", 5),
            MakeOtherCalendar("alice@example.com", 2));

        List<OtherCalendarResponse> result = await _sut.GetOtherCalendarsAsync("alice@example.com");

        Assert.Equal(2, result[0].CalendarId);
        Assert.Equal(5, result[1].CalendarId);
    }

    /// <summary>Verifies that <c>GetOtherCalendarsAsync</c> returns empty when email not found.</summary>
    [Fact]
    public async Task GetOtherCalendarsAsync_ReturnsEmpty_WhenEmailNotFound()
    {
        List<OtherCalendarResponse> result = await _sut.GetOtherCalendarsAsync("ghost@example.com");

        Assert.Empty(result);
    }

    // ── CreateOtherCalendarAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateOtherCalendarAsync</c> persists entry.</summary>
    [Fact]
    public async Task CreateOtherCalendarAsync_PersistsEntry()
    {
        var request = new OtherCalendarRequest
        {
            AccountEmail = "carol@example.com",
            CalendarId = 10
        };

        await _sut.CreateOtherCalendarAsync(request);

        bool exists = await _context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == "carol@example.com" && oc.CalendarId == 10, TestContext.Current.CancellationToken);
        Assert.True(exists);
    }

    // ── DeleteOtherCalendarAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteOtherCalendarAsync</c> removes entry.</summary>
    [Fact]
    public async Task DeleteOtherCalendarAsync_RemovesEntry()
    {
        await SeedAsync(MakeOtherCalendar("dave@example.com", 7));

        var request = new OtherCalendarRequest
        {
            AccountEmail = "dave@example.com",
            CalendarId = 7
        };

        await _sut.DeleteOtherCalendarAsync(request);

        bool exists = await _context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == "dave@example.com" && oc.CalendarId == 7, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }
}
