using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

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

    public CalendarSharedRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarSharedRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CalendarShared MakeShared(long calendarId, bool user = false, bool anonymous = false) => new()
    {
        CalendarId = calendarId,
        User = user,
        Anonymous = anonymous
    };

    private async Task SeedAsync(params CalendarShared[] entries)
    {
        await _context.CalendarShareds.AddRangeAsync(entries);
        await _context.SaveChangesAsync();
    }

    // ── GetCalendarSharedAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetCalendarSharedAsync_ReturnsAllEntries_OrderedByCalendarId()
    {
        await SeedAsync(
            MakeShared(3, user: true),
            MakeShared(1, anonymous: true),
            MakeShared(2));

        List<CalendarSharedResponse> result = await _sut.GetCalendarSharedAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].CalendarId);
        Assert.Equal(2, result[1].CalendarId);
        Assert.Equal(3, result[2].CalendarId);
    }

    [Fact]
    public async Task GetCalendarSharedAsync_ReturnsEmpty_WhenNoEntriesExist()
    {
        List<CalendarSharedResponse> result = await _sut.GetCalendarSharedAsync();

        Assert.Empty(result);
    }

    // ── CreateCalendarSharedAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateCalendarSharedAsync_PersistsEntry()
    {
        var request = new CalendarSharedRequest
        {
            CalendarId = 10,
            User = true,
            Anonymous = false
        };

        await _sut.CreateCalendarSharedAsync(request);

        CalendarShared? saved = await _context.CalendarShareds.FindAsync(10L);
        Assert.NotNull(saved);
        Assert.True(saved.User);
        Assert.False(saved.Anonymous);
    }

    // ── UpdateCalendarSharedAsync ────────────────────────────────────────────

    [Fact]
    public async Task UpdateCalendarSharedAsync_UpdatesExistingEntry()
    {
        await SeedAsync(MakeShared(20, user: false, anonymous: false));

        var request = new CalendarSharedRequest
        {
            CalendarId = 20,
            User = true,
            Anonymous = true
        };

        await _sut.UpdateCalendarSharedAsync(request);

        CalendarShared? updated = await _context.CalendarShareds.FindAsync(20L);
        Assert.NotNull(updated);
        Assert.True(updated.User);
        Assert.True(updated.Anonymous);
    }
}
