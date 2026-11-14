using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>Unit tests for <see cref="IncomeClassSuggestionPolicy"/>: which past incomes a class suggestion may learn from.</summary>
public class IncomeClassSuggestionPolicyTests
{
    /// <summary>The first record's timestamp.</summary>
    private static readonly DateTime _start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>A stored income with the given content and class, <paramref name="day"/> days after <see cref="_start"/>.</summary>
    private static Income Past(string? content, string mainClass, string subClass, int day = 0) =>
        Income.Reconstitute(day + 1, "user@example.com", mainClass, subClass, content, 1000m, "KRW", "Wallet", null,
            _start.AddDays(day), _start.AddDays(day));

    /// <summary>A learnable record carries its normalized content and its class.</summary>
    [Fact]
    public void Learnable_CarriesTheNormalizedContentAndTheClass()
    {
        var sample = Assert.Single(IncomeClassSuggestionPolicy.Learnable([Past(" Monthly  Salary", "RegularIncome", "LaborIncome", 3)]));

        Assert.Equal(new ClassSample("monthly salary", new ClassSuggestion("RegularIncome", "LaborIncome")), sample);
    }

    /// <summary>
    /// A record whose stored class is not in the income taxonomy (it would fail <see cref="IncomeClassPolicy.Validate"/>) is never
    /// learned from, and neither is one whose content has no evidence.
    /// </summary>
    [Fact]
    public void Learnable_SkipsInvalidClassesAndContentWithoutEvidence()
    {
        var samples = IncomeClassSuggestionPolicy.Learnable(
        [
            Past("급여", "RegularIncome", "Tax"),
            Past(null, "RegularIncome", "LaborIncome", 1),
            Past("--", "RegularIncome", "LaborIncome", 2),
            Past("이자", "IrregularIncome", "OtherIncome", 3),
        ]);

        Assert.Equal(["이자"], samples.Select(s => s.Text));
    }
}
