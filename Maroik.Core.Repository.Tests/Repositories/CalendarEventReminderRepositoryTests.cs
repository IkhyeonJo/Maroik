using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendarEventReminder = Maroik.Core.PostgreSQL.Models.CalendarEventReminder;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventReminderRepository"/> using an EF Core InMemory database.
/// </summary>
public class CalendarEventReminderRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventReminderRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarEventReminderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarEventReminderRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static EfCalendarEventReminder MakeReminder(long calendarEventId, string method = "Email",
        long minutesBefore = 30) => new()
    {
        CalendarEventId = calendarEventId,
        Method = method,
        MinutesBeforeEvent = minutesBefore
    };

    private async Task SeedAsync(params EfCalendarEventReminder[] reminders)
    {
        await _context.CalendarEventReminders.AddRangeAsync(reminders);
        await _context.SaveChangesAsync();
    }

    // ── GetCalendarEventRemindersAsync ───────────────────────────────────────

    /// <summary>Verifies that <c>GetCalendarEventRemindersAsync</c> returns only reminders for given event.</summary>
    [Fact]
    public async Task GetCalendarEventRemindersAsync_ReturnsOnlyRemindersForGivenEvent()
    {
        await SeedAsync(
            MakeReminder(1, "Email", 15),
            MakeReminder(1, "Push", 60),
            MakeReminder(2, "Email", 30));

        IEnumerable<CalendarEventReminder> result =
            await _sut.GetCalendarEventRemindersAsync(1);

        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.Equal(1, r.CalendarEventId));
    }

    /// <summary>Verifies that <c>GetCalendarEventRemindersAsync</c> returns empty when no reminders for event.</summary>
    [Fact]
    public async Task GetCalendarEventRemindersAsync_ReturnsEmpty_WhenNoRemindersForEvent()
    {
        IEnumerable<CalendarEventReminder> result =
            await _sut.GetCalendarEventRemindersAsync(99);

        Assert.Empty(result);
    }

    // ── CreateCalendarEventReminderAsync ─────────────────────────────────────

    /// <summary>Verifies that <c>CreateCalendarEventReminderAsync</c> persists reminder.</summary>
    [Fact]
    public async Task CreateCalendarEventReminderAsync_PersistsReminder()
    {
        var reminder = CalendarEventReminder.Reconstitute(
            id: 0,
            calendarEventId: 5,
            method: "Push",
            minutesBefore: null,
            hoursBefore: 2,
            daysBefore: null,
            weeksBefore: null,
            timesBefore: null);

        await _sut.CreateCalendarEventReminderAsync(reminder);

        EfCalendarEventReminder? saved = await _context.CalendarEventReminders
            .FirstOrDefaultAsync(r => r.CalendarEventId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Push", saved.Method);
        Assert.Equal(2, saved.HoursBeforeEvent);
    }

    // ── DeleteCalendarEventReminderAsync ─────────────────────────────────────

    /// <summary>Verifies that <c>DeleteCalendarEventReminderAsync</c> removes reminders by event id.</summary>
    [Fact]
    public async Task DeleteCalendarEventReminderAsync_RemovesRemindersByEventId()
    {
        await SeedAsync(
            MakeReminder(10, "Email", 10),
            MakeReminder(10, "Push", 20),
            MakeReminder(20, "Email", 30));

        int deleted = await _sut.DeleteCalendarEventReminderAsync(10);

        Assert.Equal(2, deleted);
        Assert.False(await _context.CalendarEventReminders.AnyAsync(r => r.CalendarEventId == 10, TestContext.Current.CancellationToken));
        Assert.True(await _context.CalendarEventReminders.AnyAsync(r => r.CalendarEventId == 20, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteCalendarEventReminderAsync</c> returns zero when no reminders match.</summary>
    [Fact]
    public async Task DeleteCalendarEventReminderAsync_ReturnsZero_WhenNoRemindersMatch()
    {
        int deleted = await _sut.DeleteCalendarEventReminderAsync(999);

        Assert.Equal(0, deleted);
    }
}
