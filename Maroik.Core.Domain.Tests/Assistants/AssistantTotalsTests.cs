using Maroik.Core.Domain.Assistants;

namespace Maroik.Core.Domain.Tests.Assistants;

/// <summary>The sums the AI chat reports are added up by the server, per group and currency, never by the model.</summary>
public class AssistantTotalsTests
{
    /// <summary>Amounts are summed and counted per group and currency, in group then currency order; currencies are never mixed.</summary>
    [Fact]
    public void Sum_AddsUpPerGroupAndCurrency()
    {
        IReadOnlyList<AssistantTotal> totals = AssistantTotals.Sum(
        [
            ("Meal", "KRW", 9000.50m),
            ("Housing", "KRW", 500000m),
            ("Meal", "USD", 12m),
            ("Meal", "KRW", 1000.25m),
        ]);

        Assert.Equal(
        [
            new AssistantTotal("Housing", "KRW", 500000m, 1),
            new AssistantTotal("Meal", "KRW", 10000.75m, 2),
            new AssistantTotal("Meal", "USD", 12m, 1),
        ], totals);
    }

    /// <summary>Nothing to add up gives no totals.</summary>
    [Fact]
    public void Sum_OfNothing_IsEmpty()
    {
        Assert.Empty(AssistantTotals.Sum([]));
    }
}
