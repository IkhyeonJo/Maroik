namespace Maroik.Core.Domain.Budgets;

/// <summary>A budget cell against the actuals.</summary>
/// <param name="Budget">The budget (the cell's number, rounded to four decimals), or null when the cell holds no number.</param>
/// <param name="Actual">The actual total of the line in the month.</param>
/// <param name="Remaining">Budget minus actual, or null without a budget.</param>
/// <param name="State">Where the actual stands against the budget.</param>
public sealed record BudgetCellStatus(decimal? Budget, decimal Actual, decimal? Remaining, BudgetCellState State)
{
    /// <summary>How the state reads on a sheet of <paramref name="kind"/>: spending below budget and earning up to it are good.</summary>
    public BudgetCellOutcome OutcomeFor(BudgetKind kind) => (kind, State) switch
    {
        (_, BudgetCellState.Unbudgeted) => BudgetCellOutcome.Unbudgeted,
        (BudgetKind.Expenditure, BudgetCellState.Under) or (BudgetKind.Income, BudgetCellState.Reached or BudgetCellState.Over) => BudgetCellOutcome.Good,
        (BudgetKind.Expenditure, BudgetCellState.Reached) or (BudgetKind.Income, BudgetCellState.Under) => BudgetCellOutcome.Warning,
        (BudgetKind.Expenditure, BudgetCellState.Over) => BudgetCellOutcome.Bad,
        _ => BudgetCellOutcome.None,
    };
}
