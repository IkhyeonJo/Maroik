using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Tests.Fixtures;

namespace Maroik.Core.Domain.Tests.Budgets;

/// <summary>Unit tests for the persisted budget aggregates: <see cref="BudgetSheet"/>, <see cref="BudgetSheetLine"/> and <see cref="BudgetSheetCell"/>.</summary>
public class BudgetAggregateTests
{
    /// <summary>The fixed "current time".</summary>
    private static readonly DateTime _now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A sheet keeps its owner, kind and year; its name is the formula name.</summary>
    [Fact]
    public void ASheet_IsCreated()
    {
        BudgetSheet sheet = BudgetSheet.Create("User@Example.com", BudgetKind.Expenditure, 2026, _now).Value;

        Assert.Equal(("user@example.com", BudgetKind.Expenditure, 2026, "Expenditure2026"), (sheet.AccountEmail.Value, sheet.Kind, sheet.Year, sheet.Name));
        Assert.Equal((_now, _now), (sheet.Created, sheet.Updated));
    }

    /// <summary>A year outside 1..9999, an unknown kind or a bad e-mail is refused.</summary>
    [Fact]
    public void ASheet_RefusesBadValues()
    {
        ErrorAssert.Validation(BudgetSheet.Create("user@example.com", BudgetKind.Income, 0, _now), "Budget.YearInvalid", "The year must be between {0} and {1}.", 1, 9999);
        ErrorAssert.Validation(BudgetSheet.Create("user@example.com", BudgetKind.Income, 10000, _now), "Budget.YearInvalid", "The year must be between {0} and {1}.", 1, 9999);
        ErrorAssert.Validation(BudgetSheet.Create("user@example.com", (BudgetKind)5, 2026, _now), "Budget.KindInvalid", "Unknown budget kind.");
        Assert.True(BudgetSheet.Create("", BudgetKind.Income, 2026, _now).IsError);
    }

    /// <summary>Touching a sheet stamps its update; reconstitution keeps stored values.</summary>
    [Fact]
    public void ASheet_IsTouched_AndReconstituted()
    {
        BudgetSheet sheet = BudgetSheet.Reconstitute(7, "user@example.com", "Income", 2025, _now.AddDays(-1), _now.AddDays(-1));

        sheet.Touch(_now);

        Assert.Equal((7L, BudgetKind.Income, 2025, _now), (sheet.Id, sheet.Kind, sheet.Year, sheet.Updated));
    }

    /// <summary>A line row belongs to a sheet and carries its line.</summary>
    [Fact]
    public void ALine_IsCreated_AndReconstituted()
    {
        BudgetLine line = BudgetLine.Create(BudgetKind.Expenditure, "ConsumerSpending", "PinMoney", "KRW").Value;

        BudgetSheetLine created = BudgetSheetLine.Create(3, line, _now);
        BudgetSheetLine stored = BudgetSheetLine.Reconstitute(9, 3, "ConsumerSpending", "PinMoney", "KRW", _now);

        Assert.Equal((0L, 3L, line, _now), (created.Id, created.SheetId, created.Line, created.Created));
        Assert.Equal((9L, line), (stored.Id, stored.Line));
    }

    /// <summary>A cell row keeps its input; changing it stamps the update.</summary>
    [Fact]
    public void ACell_IsCreated_AndChanged()
    {
        BudgetSheetCell cell = BudgetSheetCell.Create(3, "F.1", "B", "=1", _now);

        cell.ChangeInput("2", _now.AddHours(1));

        Assert.Equal((3L, "F.1", "B", "2", _now.AddHours(1)), (cell.SheetId, cell.RowKey, cell.Column, cell.Input, cell.Updated));
        Assert.Equal("x", BudgetSheetCell.Reconstitute(1, 3, "F.2", "C", "x", _now).Input);
    }

    /// <summary>
    /// An account keeps at most <see cref="BudgetSheet.MaxPerAccount"/> sheets: every budget request loads the cells of all of
    /// them, so their number bounds what one request reads.
    /// </summary>
    [Fact]
    public void AnAccount_TakesNoSheet_BeyondTheMaximum()
    {
        Assert.Equal(60, BudgetSheet.MaxPerAccount);

        Assert.False(BudgetSheet.CanAddTo(0).IsError);
        Assert.False(BudgetSheet.CanAddTo(BudgetSheet.MaxPerAccount - 1).IsError);
        ErrorAssert.Validation(BudgetSheet.CanAddTo(BudgetSheet.MaxPerAccount),
            "Budget.TooManySheets", "An account can keep at most {0} budget sheets.", BudgetSheet.MaxPerAccount);
    }
}
