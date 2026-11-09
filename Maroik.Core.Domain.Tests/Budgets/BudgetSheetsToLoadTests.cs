using Maroik.Core.Domain.Budgets;

namespace Maroik.Core.Domain.Tests.Budgets;

/// <summary>Which sheets a workbook loads for the sheet asked for.</summary>
public class BudgetSheetsToLoadTests
{
    /// <summary>The sheet itself and last year's are always there, with no duplicates.</summary>
    [Fact]
    public void TheSheetAndLastYears_AreAlwaysLoaded_Once()
    {
        var sheets = BudgetWorkbook.SheetsToLoad(BudgetKind.Income, 2025, [(BudgetKind.Income, 2025)], [], []);

        Assert.Equal([(BudgetKind.Income, 2025), (BudgetKind.Income, 2024)], sheets);
    }

    /// <summary>A year outside the valid range is dropped: the year before the first one does not exist.</summary>
    [Fact]
    public void AYearOutsideTheRange_IsDropped()
    {
        var sheets = BudgetWorkbook.SheetsToLoad(BudgetKind.Income, BudgetSheet.MinYear, [], [], []);

        Assert.Equal([(BudgetKind.Income, BudgetSheet.MinYear)], sheets);
    }

    /// <summary>However many sheets are stored, a workbook loads at most the limit, the asked-for sheet first.</summary>
    [Fact]
    public void AtMostTheLimit_IsLoaded()
    {
        var stored = Enumerable.Range(1000, BudgetWorkbook.MaxSheetsLoaded * 2).Select(y => (BudgetKind.Income, y));

        var sheets = BudgetWorkbook.SheetsToLoad(BudgetKind.Income, 2025, stored, [], []);

        Assert.Equal(BudgetWorkbook.MaxSheetsLoaded, sheets.Count);
        Assert.Equal((BudgetKind.Income, 2025), sheets[0]);
    }
}
