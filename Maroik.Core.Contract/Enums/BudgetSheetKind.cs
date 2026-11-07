namespace Maroik.Core.Contract.Enums;

/// <summary>Which budget sheet a request is about, as the callers of <c>IBudgetService</c> name it; the service maps it to the Domain's kind.</summary>
public enum BudgetSheetKind
{
    /// <summary>The Income Budget sheet.</summary>
    Income,

    /// <summary>The Expenditure Budget sheet.</summary>
    Expenditure,
}
