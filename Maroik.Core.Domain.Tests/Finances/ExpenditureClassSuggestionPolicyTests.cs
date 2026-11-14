using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>Unit tests for <see cref="ExpenditureClassSuggestionPolicy"/>: which past expenditures a class suggestion may learn from.</summary>
public class ExpenditureClassSuggestionPolicyTests
{
    /// <summary>The first record's timestamp.</summary>
    private static readonly DateTime _start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A stored expenditure with the given content and class, <paramref name="day"/> days after <see cref="_start"/>.</summary>
    private static Expenditure Past(string? content, string mainClass, string subClass, int day = 0) =>
        Expenditure.Reconstitute(day + 1, "user@example.com", mainClass, subClass, content, 1000m, "KRW", "Wallet", null, null,
            _start.AddDays(day), _start.AddDays(day));

    /// <summary>A learnable record carries its normalized content and its class.</summary>
    [Fact]
    public void Learnable_CarriesTheNormalizedContentAndTheClass()
    {
        var sample = Assert.Single(ExpenditureClassSuggestionPolicy.Learnable([Past(" Starbucks  Seoul", "ConsumerSpending", "MealOrEatOutExpenses", 3)]));

        Assert.Equal(new ClassSample("starbucks seoul", new ClassSuggestion("ConsumerSpending", "MealOrEatOutExpenses")), sample);
    }

    /// <summary>
    /// A record whose stored class is not in the expenditure taxonomy (it would fail <see cref="ExpenditureClassPolicy.Validate"/>)
    /// is never learned from — a suggestion is always a pair the user could save — and neither is one whose content has no evidence.
    /// </summary>
    [Fact]
    public void Learnable_SkipsInvalidClassesAndContentWithoutEvidence()
    {
        var samples = ExpenditureClassSuggestionPolicy.Learnable(
        [
            Past("스타벅스", "ConsumerSpending", "Tax"),
            Past(null, "ConsumerSpending", "MealOrEatOutExpenses", 1),
            Past("...", "ConsumerSpending", "MealOrEatOutExpenses", 2),
            Past("지하철", "ConsumerSpending", "TransportationCost", 3),
        ]);

        Assert.Equal(["지하철"], samples.Select(s => s.Text));
    }
}
