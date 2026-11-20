namespace Maroik.Core.Domain.Finances;

/// <summary>Which past expenditures the expenditure class suggestion learns from (the shared rules: <see cref="ClassSuggestionPolicy"/>).</summary>
public static class ExpenditureClassSuggestionPolicy
{
    /// <summary>
    /// The records of <paramref name="history"/> a suggestion may learn from, in their order, with normalized content: a class that
    /// passes <see cref="ExpenditureClassPolicy.Validate"/> and content with evidence.
    /// </summary>
    public static IReadOnlyList<ClassSample> Learnable(IEnumerable<Expenditure> history) =>
    [
        .. history
            .Where(e => ClassSuggestionPolicy.HasEvidence(e.Content) && !ExpenditureClassPolicy.Validate(e.MainClass, e.SubClass).IsError)
            .Select(e => new ClassSample(ClassSuggestionPolicy.Normalize(e.Content), new ClassSuggestion(e.MainClass, e.SubClass)))
    ];
}
