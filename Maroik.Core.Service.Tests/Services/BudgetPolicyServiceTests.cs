using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Formulas;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="BudgetPolicyService"/>: each member answers what the Domain answers.</summary>
public class BudgetPolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly BudgetPolicyService _sut = new();

    /// <summary>Each sheet kind gets its own taxonomy, and the columns follow the layout.</summary>
    [Theory]
    [InlineData(BudgetSheetKind.Income, BudgetKind.Income)]
    [InlineData(BudgetSheetKind.Expenditure, BudgetKind.Expenditure)]
    public void GetBudgetPageRules_CarriesTheKindsTaxonomyAndTheLayout(BudgetSheetKind kind, BudgetKind domainKind)
    {
        BudgetPageRules rules = _sut.GetBudgetPageRules(kind);

        Assert.Equal(BudgetLine.MainClasses(domainKind), rules.MainClasses);
        Assert.Equal(BudgetLine.Taxonomy(domainKind), rules.SubClassesByMainClass);
        Assert.Equal(BudgetSheetLayout.YearColumn, rules.YearColumn);
        Assert.Equal(12, rules.MonthByColumn.Count);
        Assert.Equal(1, rules.MonthByColumn["B"]);
        Assert.Equal(12, rules.MonthByColumn["M"]);
        Assert.Equal(
            FormulaFunctions.All.Select(f => (f.Name, f.MinArguments, f.MaxArguments, f.Signature)),
            rules.Functions.Select(f => (f.Name, f.MinArguments, f.MaxArguments, f.Signature)));
    }

}
