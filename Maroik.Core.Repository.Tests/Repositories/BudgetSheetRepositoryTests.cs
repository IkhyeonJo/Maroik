using Maroik.Core.Domain.Budgets;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for the budget repositories (<see cref="BudgetSheetRepository"/>, <see cref="BudgetSheetLineRepository"/>,
/// <see cref="BudgetSheetCellRepository"/>) against a real PostgreSQL database with the <c>Init.sql</c> schema: a sheet created with its
/// lines, the owner-scoped lookups and the row lock, cell inputs written, read and deleted, and the constraints (one sheet per kind and
/// year, unique lines and cells, valid classes, keys and columns, cascading deletes).
/// </summary>
public sealed class BudgetSheetRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    /// <summary>The fixed time the rows are stamped with.</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The sheet repository.</summary>
    private BudgetSheetRepository Sheets => new(Context);

    /// <summary>The line repository.</summary>
    private BudgetSheetLineRepository Lines => new(Context);

    /// <summary>The cell repository.</summary>
    private BudgetSheetCellRepository Cells => new(Context);

    /// <summary>Meal / eat-out in KRW.</summary>
    private static readonly BudgetLine _meal = BudgetLine.Create(BudgetKind.Expenditure, "ConsumerSpending", "MealOrEatOutExpenses", "KRW").Value;

    /// <summary>Pin money in USD.</summary>
    private static readonly BudgetLine _pin = BudgetLine.Create(BudgetKind.Expenditure, "ConsumerSpending", "PinMoney", "USD").Value;

    /// <summary>Creates a sheet with lines for a fresh account and returns its id.</summary>
    private async Task<(string Email, long Id)> CreateSheetAsync(BudgetKind kind = BudgetKind.Expenditure, int year = 2026, string? email = null, params BudgetLine[] lines)
    {
        email ??= UniqueEmail();
        await EnsureAccountsAsync(email);
        await Sheets.CreateWithLinesAsync(BudgetSheet.Create(email, kind, year, _now).Value, lines, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        long id = await Context.BudgetSheets.AsNoTracking().Where(s => s.AccountEmail == email && s.Kind == kind.ToString() && s.Year == year).Select(s => s.Id).SingleAsync(TestContext.Current.CancellationToken);
        return (email, id);
    }

    /// <summary>A sheet is created with its lines; the account's sheets read back with every field.</summary>
    [Fact]
    public async Task CreateWithLinesAsync_StoresTheSheetAndItsLines()
    {
        var (email, id) = await CreateSheetAsync(lines: [_meal, _pin]);

        BudgetSheet sheet = Assert.Single(await Sheets.GetByAccountEmailAsync(email, TestContext.Current.CancellationToken));
        Assert.Equal((id, email, BudgetKind.Expenditure, 2026, _now, _now), (sheet.Id, sheet.AccountEmail.Value, sheet.Kind, sheet.Year, sheet.Created, sheet.Updated));

        List<BudgetSheetLine> lines = await Lines.GetBySheetIdsAsync([id], TestContext.Current.CancellationToken);
        Assert.Equal([_meal, _pin], lines.Select(l => l.Line).OrderBy(l => l.SubClass, StringComparer.Ordinal));
        Assert.All(lines, l => Assert.Equal((id, _now), (l.SheetId, l.Created)));
    }

    /// <summary>The lock finds the owner's sheet of a kind and year, and nothing for another account.</summary>
    [Fact]
    public async Task FindForUpdateAsync_IsOwnerScoped()
    {
        var (email, id) = await CreateSheetAsync(year: 2025);
        await using var transaction = await Context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.Equal(id, (await Sheets.FindForUpdateAsync(email, BudgetKind.Expenditure, 2025, TestContext.Current.CancellationToken))!.Id);
        Assert.Null(await Sheets.FindForUpdateAsync(email, BudgetKind.Income, 2025, TestContext.Current.CancellationToken));
        Assert.Null(await Sheets.FindForUpdateAsync(UniqueEmail("other"), BudgetKind.Expenditure, 2025, TestContext.Current.CancellationToken));
    }

    /// <summary>A sheet's update stamp is saved.</summary>
    [Fact]
    public async Task UpdateEntityAsync_SavesTheUpdateStamp()
    {
        var (email, _) = await CreateSheetAsync();
        BudgetSheet sheet = (await Sheets.GetByAccountEmailAsync(email, TestContext.Current.CancellationToken)).Single();

        sheet.Touch(_now.AddDays(1));
        await Sheets.UpdateEntityAsync(sheet, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal(_now.AddDays(1), (await Sheets.GetByAccountEmailAsync(email, TestContext.Current.CancellationToken)).Single().Updated);
    }

    /// <summary>One sheet per account, kind and year.</summary>
    [Fact]
    public async Task ASecondSheetOfTheSameKindAndYear_IsRefused()
    {
        var (email, _) = await CreateSheetAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() => Sheets.CreateWithLinesAsync(BudgetSheet.Create(email, BudgetKind.Expenditure, 2026, _now).Value, [], TestContext.Current.CancellationToken));
    }

    /// <summary>A line is added and deleted; the same line twice is refused.</summary>
    [Fact]
    public async Task Lines_AreAddedAndDeleted()
    {
        var (_, id) = await CreateSheetAsync(lines: [_meal]);

        await Lines.CreateAsync(BudgetSheetLine.Create(id, _pin, _now), TestContext.Current.CancellationToken);
        await Lines.DeleteAsync(id, _meal, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal([_pin], (await Lines.GetBySheetIdsAsync([id], TestContext.Current.CancellationToken)).Select(l => l.Line));
        Context.ChangeTracker.Clear();
        await Assert.ThrowsAsync<DbUpdateException>(() => Lines.CreateAsync(BudgetSheetLine.Create(id, _pin, _now), TestContext.Current.CancellationToken));
    }

    /// <summary>A class the schema does not know is refused by its check.</summary>
    [Fact]
    public async Task AnUnknownClass_IsRefusedByTheSchema()
    {
        var (_, id) = await CreateSheetAsync();

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => Lines.CreateAsync(BudgetSheetLine.Create(id, new BudgetLine("Nope", "PinMoney", "KRW"), _now), TestContext.Current.CancellationToken));
        Assert.Equal("BudgetSheetLine_MainClass_check", Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
    }

    /// <summary>Cell inputs are written, found, changed and deleted (by cell and by row); reads cover several sheets.</summary>
    [Fact]
    public async Task Cells_AreWrittenReadAndDeleted()
    {
        var (email, id) = await CreateSheetAsync(lines: [_meal]);
        var (_, other) = await CreateSheetAsync(BudgetKind.Income, 2026, email);

        await Cells.CreateAsync(BudgetSheetCell.Create(id, _meal.Key, "B", "300000", _now), TestContext.Current.CancellationToken);
        await Cells.CreateAsync(BudgetSheetCell.Create(id, _meal.Key, "C", "=B{" + _meal.Key + "}", _now), TestContext.Current.CancellationToken);
        await Cells.CreateAsync(BudgetSheetCell.Create(id, "F.1", "B", "note", _now), TestContext.Current.CancellationToken);
        await Cells.CreateAsync(BudgetSheetCell.Create(other, "F.1", "A", "x", _now), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        BudgetSheetCell found = (await Cells.FindAsync(id, "F.1", "B", TestContext.Current.CancellationToken))!;
        found.ChangeInput("changed", _now.AddHours(1));
        await Cells.UpdateEntityAsync(found, TestContext.Current.CancellationToken);
        await Cells.DeleteAsync(id, _meal.Key, "C", TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        List<BudgetSheetCell> cells = await Cells.GetBySheetIdsAsync([id, other], TestContext.Current.CancellationToken);
        Assert.Equal([("F.1", "A", "x"), ("F.1", "B", "changed"), (_meal.Key, "B", "300000")],
            cells.OrderBy(c => c.Column, StringComparer.Ordinal).ThenBy(c => c.RowKey, StringComparer.Ordinal).Select(c => (c.RowKey, c.Column, c.Input)));
        Assert.Null(await Cells.FindAsync(id, _meal.Key, "C", TestContext.Current.CancellationToken));

        await Cells.DeleteRowAsync(id, _meal.Key, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();
        Assert.Equal(["changed"], (await Cells.GetBySheetIdsAsync([id], TestContext.Current.CancellationToken)).Select(c => c.Input));
    }

    /// <summary>The same cell twice, a bad column or a bad row key is refused by the schema.</summary>
    [Theory]
    [InlineData("F.1", "B", "BudgetSheetCell_unique")]
    [InlineData("F.1", "a", "BudgetSheetCell_Column_check")]
    [InlineData("F 1", "C", "BudgetSheetCell_RowKey_check")]
    public async Task BadCells_AreRefusedByTheSchema(string rowKey, string column, string constraint)
    {
        var (_, id) = await CreateSheetAsync();
        await Cells.CreateAsync(BudgetSheetCell.Create(id, "F.1", "B", "1", _now), TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => Cells.CreateAsync(BudgetSheetCell.Create(id, rowKey, column, "2", _now), TestContext.Current.CancellationToken));
        Assert.Equal(constraint, Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
    }

    /// <summary>Deleting a sheet deletes its lines and cells (ON DELETE CASCADE).</summary>
    [Fact]
    public async Task DeletingASheet_CascadesToItsLinesAndCells()
    {
        var (_, id) = await CreateSheetAsync(lines: [_meal]);
        await Cells.CreateAsync(BudgetSheetCell.Create(id, "F.1", "B", "1", _now), TestContext.Current.CancellationToken);

        await Context.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "BudgetSheet" WHERE "Id" = {id}""", TestContext.Current.CancellationToken);

        Assert.Empty(await Lines.GetBySheetIdsAsync([id], TestContext.Current.CancellationToken));
        Assert.Empty(await Cells.GetBySheetIdsAsync([id], TestContext.Current.CancellationToken));
    }
}
