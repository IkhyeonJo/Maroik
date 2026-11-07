using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Formulas;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IBudgetPolicyService"/>
public sealed class BudgetPolicyService : IBudgetPolicyService
{
    /// <summary>The month columns of a budget sheet, in column order.</summary>
    private static readonly IReadOnlyDictionary<string, int> _monthByColumn =
        Enumerable.Range(0, BudgetSheetLayout.ColumnCount)
            .Select(i => ((char)('A' + i)).ToString())
            .Select(column => (column, month: BudgetSheetLayout.MonthOf(column)))
            .Where(c => c.month is not null)
            .ToDictionary(c => c.column, c => c.month!.Value, StringComparer.Ordinal);

    /// <summary>The functions the formula editor offers.</summary>
    private static readonly IReadOnlyList<BudgetFunctionResponse> _budgetFunctions =
        [.. FormulaFunctions.All.Select(f => new BudgetFunctionResponse(f.Name, f.MinArguments, f.MaxArguments, f.Signature))];

    /// <inheritdoc />
    public BudgetPageRules GetBudgetPageRules(BudgetSheetKind kind)
    {
        BudgetKind domainKind = BudgetMapper.ToDomain(kind);
        return new BudgetPageRules
        {
            MainClasses = BudgetLine.MainClasses(domainKind),
            SubClassesByMainClass = BudgetLine.Taxonomy(domainKind),
            MonthByColumn = _monthByColumn,
            YearColumn = BudgetSheetLayout.YearColumn,
            Functions = _budgetFunctions,
        };
    }
}
