using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Filters;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarSharedRepository"/> using an EF Core InMemory database.
/// CalendarShared has CalendarId as its primary key (1-to-1 with Calendar).
/// FK constraints are not enforced by InMemory, so Calendar parent records are omitted.
/// </summary>
public class CalendarSharedRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarSharedRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarSharedRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarSharedRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfCalendarShared MakeShared(long calendarId, bool user = false, bool anonymous = false) => new()
    {
        CalendarId = calendarId,
        User = user,
        Anonymous = anonymous
    };

    private async Task SeedAsync(params EfCalendarShared[] entries)
    {
        await _context.CalendarShareds.AddRangeAsync(entries);
        await _context.SaveChangesAsync();
    }

    // -- GetCalendarSharedAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetCalendarSharedAsync</c> returns all entries when ordered by calendar id.</summary>
    [Fact]
    public async Task GetCalendarSharedAsync_ReturnsAllEntries_OrderedByCalendarId()
    {
        await SeedAsync(
            MakeShared(3, user: true),
            MakeShared(1, anonymous: true),
            MakeShared(2));

        List<CalendarShared> result = await _sut.FindAsync([], "CalendarId");

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    /// <summary>Verifies that <c>GetCalendarSharedAsync</c> returns empty when no entries exist.</summary>
    [Fact]
    public async Task GetCalendarSharedAsync_ReturnsEmpty_WhenNoEntriesExist()
    {
        List<CalendarShared> result = await _sut.FindAsync([]);

        Assert.Empty(result);
    }

    // -- CreateCalendarSharedAsync --------------------------------------------

    /// <summary>Verifies that <c>CreateCalendarSharedAsync</c> persists entry.</summary>
    [Fact]
    public async Task CreateCalendarSharedAsync_PersistsEntry()
    {
        var shared = CalendarShared.Reconstitute(
            calendarId: 10,
            user: true,
            anonymous: false);

        await _sut.CreateAsync(shared);

        EfCalendarShared? saved = await _context.CalendarShareds.FindAsync(new object[] { 10L }, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.True(saved.User);
        Assert.False(saved.Anonymous);
    }

    // -- UpdateEntityAsync --------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing entry.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingEntry()
    {
        await SeedAsync(MakeShared(20, user: false, anonymous: false));

        var shared = CalendarShared.Reconstitute(
            calendarId: 20,
            user: true,
            anonymous: true);

        await _sut.UpdateEntityAsync(shared);

        EfCalendarShared? updated = await _context.CalendarShareds.FindAsync(new object[] { 20L }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.True(updated.User);
        Assert.True(updated.Anonymous);
    }
}
