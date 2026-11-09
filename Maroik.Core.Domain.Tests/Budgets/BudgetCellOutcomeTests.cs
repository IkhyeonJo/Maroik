using Maroik.Core.Domain.Budgets;

namespace Maroik.Core.Domain.Tests.Budgets;

/// <summary>How a budget cell's state reads: staying under is good for spending, and a warning for earning.</summary>
public class BudgetCellOutcomeTests
{
    /// <summary>Every kind and state.</summary>
    [Theory]
    [InlineData(BudgetKind.Expenditure, BudgetCellState.Under, BudgetCellOutcome.Good)]
    [InlineData(BudgetKind.Expenditure, BudgetCellState.Reached, BudgetCellOutcome.Warning)]
    [InlineData(BudgetKind.Expenditure, BudgetCellState.Over, BudgetCellOutcome.Bad)]
    [InlineData(BudgetKind.Income, BudgetCellState.Under, BudgetCellOutcome.Warning)]
    [InlineData(BudgetKind.Income, BudgetCellState.Reached, BudgetCellOutcome.Good)]
    [InlineData(BudgetKind.Income, BudgetCellState.Over, BudgetCellOutcome.Good)]
    [InlineData(BudgetKind.Income, BudgetCellState.Unbudgeted, BudgetCellOutcome.Unbudgeted)]
    [InlineData(BudgetKind.Expenditure, BudgetCellState.Unbudgeted, BudgetCellOutcome.Unbudgeted)]
    [InlineData(BudgetKind.Income, BudgetCellState.None, BudgetCellOutcome.None)]
    [InlineData(BudgetKind.Expenditure, BudgetCellState.None, BudgetCellOutcome.None)]
    public void OutcomeFor_ReadsTheStateByKind(BudgetKind kind, BudgetCellState state, BudgetCellOutcome expected)
    {
        Assert.Equal(expected, new BudgetCellStatus(null, 0m, null, state).OutcomeFor(kind));
    }
}
