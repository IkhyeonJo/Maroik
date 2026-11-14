using Maroik.Core.Domain.Finances;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="ClassModelRepository"/> against a real PostgreSQL database (Testcontainers) preloaded with the <c>Init.sql</c>
/// schema: a model record is found by account and book, a retraining updates the same row, and the schema keeps one record per
/// account and book (<c>ClassModel_unique</c>) of the two books only (<c>ClassModel_Book_check</c>).
/// </summary>
public sealed class ClassModelRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private ClassModelRepository Sut => new(Context);

    /// <summary>When the models of these tests were trained.</summary>
    private static readonly DateTime _trained = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Unspecified);

    /// <summary>A new model record of <paramref name="email"/>'s <paramref name="book"/>.</summary>
    private static ClassModel Model(string email, string book, string path = "upload/ClassModels/a.zip") =>
        ClassModel.Create(email, book, path, "fp1", "v1", _trained);

    /// <summary>A stored record is found by its account and book, with every field; the other book of the account has none.</summary>
    [Fact]
    public async Task FindAsync_ReturnsTheRecordOfTheAccountAndBook()
    {
        string email = UniqueEmail();
        await EnsureAccountsAsync(email);
        await Sut.CreateAsync(Model(email, ClassModel.ExpenditureBook), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        ClassModel? found = await Sut.FindAsync(email, ClassModel.ExpenditureBook, TestContext.Current.CancellationToken);
        ClassModel? other = await Sut.FindAsync(email, ClassModel.IncomeBook, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.True(found.Id > 0);
        Assert.Equal((email, ClassModel.ExpenditureBook, "upload/ClassModels/a.zip", "fp1", "v1", _trained),
            (found.AccountEmail, found.Book, found.FilePath, found.Fingerprint, found.ModelVersion, found.Trained));
        Assert.Null(other);
    }

    /// <summary>A retraining rewrites the same row: one record of the account and book, holding the new file.</summary>
    [Fact]
    public async Task UpdateEntityAsync_RewritesTheSameRow()
    {
        string email = UniqueEmail();
        await EnsureAccountsAsync(email);
        await Sut.CreateAsync(Model(email, ClassModel.IncomeBook), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        ClassModel kept = (await Sut.FindAsync(email, ClassModel.IncomeBook, TestContext.Current.CancellationToken))!;

        await Sut.UpdateEntityAsync(kept.Retrained("upload/ClassModels/b.zip", "fp2", "v2", _trained.AddHours(1)), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        ClassModel found = (await Sut.FindAsync(email, ClassModel.IncomeBook, TestContext.Current.CancellationToken))!;
        Assert.Equal((kept.Id, "upload/ClassModels/b.zip", "fp2", "v2", _trained.AddHours(1)),
            (found.Id, found.FilePath, found.Fingerprint, found.ModelVersion, found.Trained));
        Assert.Equal(1, await Context.ClassModels.CountAsync(m => m.AccountEmail == email, TestContext.Current.CancellationToken));
    }

    /// <summary>A second record of the same account and book is refused by the database.</summary>
    [Fact]
    public async Task CreateAsync_ASecondRecordOfTheSameBook_IsRefused()
    {
        string email = UniqueEmail();
        await EnsureAccountsAsync(email);
        await Sut.CreateAsync(Model(email, ClassModel.ExpenditureBook), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            Sut.CreateAsync(Model(email, ClassModel.ExpenditureBook, "upload/ClassModels/b.zip"), TestContext.Current.CancellationToken));
    }

    /// <summary>A book other than Income or Expenditure is refused by the database.</summary>
    [Fact]
    public async Task CreateAsync_AnUnknownBook_IsRefused()
    {
        string email = UniqueEmail();
        await EnsureAccountsAsync(email);

        await Assert.ThrowsAsync<DbUpdateException>(() => Sut.CreateAsync(Model(email, "Savings"), TestContext.Current.CancellationToken));
    }
}
