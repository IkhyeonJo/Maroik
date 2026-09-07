using Maroik.Core.Domain.Calendar;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfOtherCalendar = Maroik.Core.PostgreSQL.Models.OtherCalendar;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="OtherCalendarRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data (two subscriptions).
/// <c>OtherCalendar</c> has the composite PK <c>(AccountEmail, CalendarId)</c> and the FKs
/// <c>OtherCalendar_fk_0</c> to <c>Account.Email</c> and <c>OtherCalendar_fk_1</c> to
/// <c>Calendar.Id</c> (both unenforced by InMemory). The global <c>GetAllAsync</c> assertion is
/// scoped to the rows this test created.
/// </summary>
public sealed class OtherCalendarRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private OtherCalendarRepository Sut => new(Context);

    private async Task SeedAsync(string email, params long[] calendarIds)
    {
        await EnsureAccountsAsync(email);
        await Context.OtherCalendars.AddRangeAsync(calendarIds.Select(id => new EfOtherCalendar
        {
            AccountEmail = email,
            CalendarId = id
        }));
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetAllAsync --------------------------------------------

    /// <summary>Verifies that <c>GetAllAsync</c> includes every subscription this test inserted.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsInsertedEntries()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        long c1 = await InsertCalendarAsync();
        long c2 = await InsertCalendarAsync();
        long c3 = await InsertCalendarAsync();
        await SeedAsync(alice, c1, c2);
        await SeedAsync(bob, c3);

        List<OtherCalendar> result = await Sut.GetAllAsync(TestContext.Current.CancellationToken);

        string[] mine = [alice, bob];
        Assert.Equal(3, result.Count(oc => mine.Contains(oc.AccountEmail.Value)));
    }

    // -- GetByAccountEmailAsync -----------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only subscriptions for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlySubscriptionsForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        long c1 = await InsertCalendarAsync();
        long c2 = await InsertCalendarAsync();
        long c3 = await InsertCalendarAsync();
        await SeedAsync(alice, c1, c2);
        await SeedAsync(bob, c3);

        List<OtherCalendar> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(alice, r.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns empty when the e-mail is not found.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmpty_WhenEmailNotFound()
    {
        List<OtherCalendar> result = await Sut.GetByAccountEmailAsync(UniqueEmail("ghost"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateAsync ---------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts a subscription row.</summary>
    [Fact]
    public async Task CreateAsync_PersistsEntry()
    {
        string carol = UniqueEmail("carol");
        await EnsureAccountsAsync(carol);
        long calendarId = await InsertCalendarAsync();

        await Sut.CreateAsync(OtherCalendar.Reconstitute(accountEmail: carol, calendarId: calendarId), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.True(await Context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == carol && oc.CalendarId == calendarId, TestContext.Current.CancellationToken));
    }

    /// <summary>PostgreSQL enforces <c>OtherCalendar_fk_1</c> — a subscription to an unknown calendar throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenParentCalendarDoesNotExist()
    {
        string carol = UniqueEmail("carol");
        await EnsureAccountsAsync(carol);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(OtherCalendar.Reconstitute(accountEmail: carol, calendarId: long.MaxValue), TestContext.Current.CancellationToken));
    }

    // -- DeleteByAccountEmailAndCalendarIdAsync ------------------------------

    /// <summary>Verifies that <c>DeleteByAccountEmailAndCalendarIdAsync</c> removes the one matching row.</summary>
    [Fact]
    public async Task DeleteByAccountEmailAndCalendarIdAsync_RemovesEntry()
    {
        string dave = UniqueEmail("dave");
        long calendarId = await InsertCalendarAsync();
        await SeedAsync(dave, calendarId);

        await Sut.DeleteByAccountEmailAndCalendarIdAsync(dave, calendarId, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.False(await Context.OtherCalendars.AnyAsync(
            oc => oc.AccountEmail == dave && oc.CalendarId == calendarId, TestContext.Current.CancellationToken));
    }

    // -- DeleteByAccountEmailAsync --------------------------------------------

    /// <summary>Verifies that <c>DeleteByAccountEmailAsync</c> removes all subscriptions for the e-mail.</summary>
    [Fact]
    public async Task DeleteByAccountEmailAsync_RemovesAllEntriesForEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        long c1 = await InsertCalendarAsync();
        long c2 = await InsertCalendarAsync();
        long c3 = await InsertCalendarAsync();
        await SeedAsync(alice, c1, c2);
        await SeedAsync(bob, c3);

        await Sut.DeleteByAccountEmailAsync(alice, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.False(await Context.OtherCalendars.AnyAsync(oc => oc.AccountEmail == alice, TestContext.Current.CancellationToken));
        Assert.True(await Context.OtherCalendars.AnyAsync(oc => oc.AccountEmail == bob, TestContext.Current.CancellationToken));
    }
}
