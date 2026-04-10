using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventReminderRepository"/>.
/// SQLite in-memory is used throughout because <see cref="CalendarEventReminderRepository.DeleteCalendarEventReminderAsync"/>
/// calls <c>ExecuteSqlInterpolatedAsync</c>, which requires a relational provider.
/// </summary>
public class CalendarEventReminderRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventReminderRepository _sut;

    public CalendarEventReminderRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new CalendarEventReminderRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CalendarEventReminder MakeReminder(long calendarEventId, string method = "Email",
        long minutesBefore = 30) => new()
    {
        CalendarEventId = calendarEventId,
        Method = method,
        MinutesBeforeEvent = minutesBefore
    };

    private async Task SeedAsync(params CalendarEventReminder[] reminders)
    {
        await _context.CalendarEventReminders.AddRangeAsync(reminders);
        await _context.SaveChangesAsync();
    }

    // ── GetCalendarEventRemindersAsync ───────────────────────────────────────

    [Fact]
    public async Task GetCalendarEventRemindersAsync_ReturnsOnlyRemindersForGivenEvent()
    {
        await SeedAsync(
            MakeReminder(1, "Email", 15),
            MakeReminder(1, "Push", 60),
            MakeReminder(2, "Email", 30));

        IEnumerable<CalendarEventReminderResponse> result =
            await _sut.GetCalendarEventRemindersAsync(1);

        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(1, r.CalendarEventId));
    }

    [Fact]
    public async Task GetCalendarEventRemindersAsync_ReturnsEmpty_WhenNoRemindersForEvent()
    {
        IEnumerable<CalendarEventReminderResponse> result =
            await _sut.GetCalendarEventRemindersAsync(99);

        Assert.Empty(result);
    }

    // ── CreateCalendarEventReminderAsync ─────────────────────────────────────

    [Fact]
    public async Task CreateCalendarEventReminderAsync_PersistsReminder()
    {
        var request = new CalendarEventReminderRequest
        {
            CalendarEventId = 5,
            Method = "Push",
            HoursBeforeEvent = 2
        };

        await _sut.CreateCalendarEventReminderAsync(request);

        CalendarEventReminder? saved = await _context.CalendarEventReminders
            .FirstOrDefaultAsync(r => r.CalendarEventId == 5);
        Assert.NotNull(saved);
        Assert.Equal("Push", saved.Method);
        Assert.Equal(2, saved.HoursBeforeEvent);
    }

    // ── DeleteCalendarEventReminderAsync ─────────────────────────────────────

    [Fact]
    public async Task DeleteCalendarEventReminderAsync_RemovesRemindersByEventId()
    {
        await SeedAsync(
            MakeReminder(10, "Email", 10),
            MakeReminder(10, "Push", 20),
            MakeReminder(20, "Email", 30));

        int deleted = await _sut.DeleteCalendarEventReminderAsync(10);

        Assert.Equal(2, deleted);
        Assert.False(await _context.CalendarEventReminders.AnyAsync(r => r.CalendarEventId == 10));
        Assert.True(await _context.CalendarEventReminders.AnyAsync(r => r.CalendarEventId == 20));
    }

    [Fact]
    public async Task DeleteCalendarEventReminderAsync_ReturnsZero_WhenNoRemindersMatch()
    {
        int deleted = await _sut.DeleteCalendarEventReminderAsync(999);

        Assert.Equal(0, deleted);
    }
}
