using Maroik.Core.Domain.Calendar;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarSharedRepository"/> against a real PostgreSQL database
/// (Testcontainers) pre-loaded with the production <c>Init.sql</c> schema and seed data (one shared
/// calendar). <c>CalendarShared.CalendarId</c> is both the primary key and the FK
/// <c>CalendarShared_fk_0</c> to <c>Calendar.Id</c> (unenforced by InMemory), so every test creates
/// a real parent calendar; the global <c>GetAllAsync</c> assertions are scoped to the ids this test
/// created.
/// </summary>
public sealed class CalendarSharedRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private CalendarSharedRepository Sut => new(Context);

    private static EfCalendarShared MakeShared(long calendarId, bool user = false, bool anonymous = false) => new()
    {
        CalendarId = calendarId,
        User = user,
        Anonymous = anonymous
    };

    private async Task SeedAsync(params EfCalendarShared[] entries)
    {
        await Context.CalendarShareds.AddRangeAsync(entries);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetAllAsync ------------------------------------------------------

    /// <summary>Verifies that <c>GetAllAsync</c> includes every entry this test inserted.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsInsertedEntries()
    {
        long c1 = await InsertCalendarAsync();
        long c2 = await InsertCalendarAsync();
        long c3 = await InsertCalendarAsync();
        await SeedAsync(MakeShared(c1, user: true), MakeShared(c2, anonymous: true), MakeShared(c3));

        List<CalendarShared> result = await Sut.GetAllAsync(TestContext.Current.CancellationToken);

        long[] mine = [c1, c2, c3];
        Assert.Equal(3, result.Count(s => mine.Contains(s.Id)));
    }

    // -- CreateAsync --------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts a shared-calendar entry.</summary>
    [Fact]
    public async Task CreateAsync_PersistsEntry()
    {
        long calendarId = await InsertCalendarAsync();

        var shared = CalendarShared.Reconstitute(calendarId: calendarId, user: true, anonymous: false);

        await Sut.CreateAsync(shared, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfCalendarShared? saved = await Context.CalendarShareds
            .FirstOrDefaultAsync(s => s.CalendarId == calendarId, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.True(saved.User);
        Assert.False(saved.Anonymous);
    }

    /// <summary>PostgreSQL enforces <c>CalendarShared_fk_0</c> — a share for an unknown calendar throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenParentCalendarDoesNotExist()
    {
        var shared = CalendarShared.Reconstitute(calendarId: long.MaxValue, user: true, anonymous: false);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(shared, TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync --------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing entry.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingEntry()
    {
        long calendarId = await InsertCalendarAsync();
        await SeedAsync(MakeShared(calendarId, user: false, anonymous: false));

        var shared = CalendarShared.Reconstitute(calendarId: calendarId, user: true, anonymous: true);

        await Sut.UpdateEntityAsync(shared, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfCalendarShared? updated = await Context.CalendarShareds
            .FirstOrDefaultAsync(s => s.CalendarId == calendarId, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.True(updated.User);
        Assert.True(updated.Anonymous);
    }
}
