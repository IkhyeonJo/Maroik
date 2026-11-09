namespace Maroik.Core.Domain.Budgets;

/// <summary>How a budget cell's state reads on its sheet.</summary>
public enum BudgetCellOutcome
{
    /// <summary>Nothing to say: no budget and nothing recorded, or not a budget cell.</summary>
    None,

    /// <summary>Records but no budget.</summary>
    Unbudgeted,

    /// <summary>Within an expenditure budget, or an income budget reached.</summary>
    Good,

    /// <summary>An expenditure budget used up, or an income budget not reached.</summary>
    Warning,

    /// <summary>An expenditure budget exceeded.</summary>
    Bad,
}
