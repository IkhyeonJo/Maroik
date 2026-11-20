namespace Maroik.Core.Domain.Finances;

/// <summary>Which past incomes the income class suggestion learns from (the shared rules: <see cref="ClassSuggestionPolicy"/>).</summary>
public static class IncomeClassSuggestionPolicy
{
    /// <summary>
    /// The records of <paramref name="history"/> a suggestion may learn from, in their order, with normalized content: a class that
    /// passes <see cref="IncomeClassPolicy.Validate"/> and content with evidence.
    /// </summary>
    public static IReadOnlyList<ClassSample> Learnable(IEnumerable<Income> history) =>
    [
        .. history
            .Where(i => ClassSuggestionPolicy.HasEvidence(i.Content) && !IncomeClassPolicy.Validate(i.MainClass, i.SubClass).IsError)
            .Select(i => new ClassSample(ClassSuggestionPolicy.Normalize(i.Content), new ClassSuggestion(i.MainClass, i.SubClass)))
    ];
}
