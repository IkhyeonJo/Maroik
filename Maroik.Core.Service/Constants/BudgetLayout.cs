using Maroik.Core.Domain.Budgets;

namespace Maroik.Core.Service.Constants;

/// <summary>The Domain's budget sheet layout, exposed by the Service layer to the Website's Excel export, since the Website may not read the Domain.</summary>
public static class BudgetLayout
{
    /// <summary><see cref="BudgetSheetLayout.ColumnCount"/>.</summary>
    public const int ColumnCount = BudgetSheetLayout.ColumnCount;

    /// <summary><see cref="BudgetSheetLayout.FirstFreeColumnIndex"/>.</summary>
    public const int FirstFreeColumnIndex = BudgetSheetLayout.FirstFreeColumnIndex;

    /// <summary>The row kind of a free row, as the sheet response names it: <see cref="BudgetRowKind.Free"/>.</summary>
    public const string FreeRowKind = nameof(BudgetRowKind.Free);
}
