using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Formulas;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Tests.Mappers;

/// <summary>Unit tests for <see cref="BudgetMapper"/>: the budget domain's views as their DTOs.</summary>
public class BudgetMapperTests
{
    /// <summary>Every outcome the Domain judges has its DTO value.</summary>
    [Theory]
    [InlineData(BudgetCellOutcome.None, BudgetOutcome.None)]
    [InlineData(BudgetCellOutcome.Unbudgeted, BudgetOutcome.Unbudgeted)]
    [InlineData(BudgetCellOutcome.Good, BudgetOutcome.Good)]
    [InlineData(BudgetCellOutcome.Warning, BudgetOutcome.Warning)]
    [InlineData(BudgetCellOutcome.Bad, BudgetOutcome.Bad)]
    public void ToResponse_CarriesACellsOutcome(BudgetCellOutcome outcome, BudgetOutcome expected)
    {
        var cell = new BudgetCellView("B2", BudgetCellRole.Budget, null, new NumberValue(1m), null, outcome);

        Assert.Equal(expected, BudgetMapper.ToResponse(cell).Outcome);
    }
}
