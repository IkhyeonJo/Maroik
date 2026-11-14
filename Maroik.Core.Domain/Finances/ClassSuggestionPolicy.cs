namespace Maroik.Core.Domain.Finances;

/// <summary>
/// The rules shared by the class the income and expenditure forms propose for typed content, learned only from the same account's
/// own past records of that book. The proposal is a hint the form shows next to the class fields; the user applies it by hand (it
/// never fills the form by itself), and the record is validated on save like any other. Classification stays the user's decision.
/// <para>
/// Content without a letter or a digit (<see cref="HasEvidence"/>) gets no suggestion; only learnable records are learned from
/// (<see cref="IncomeClassSuggestionPolicy.Learnable"/>, <see cref="ExpenditureClassSuggestionPolicy.Learnable"/>: a class of the
/// book's taxonomy, so a suggestion is always a pair the user could save). Everything else — content recorded before included —
/// is the Service's <c>ContentClassifier</c> (a text model) to answer. A "same content as before" rule in front of the model was
/// measured and dropped (2026-11-14): with monthly retraining on the owner's newest 1,848 expenditures and 125 incomes, it was
/// never right where the model was wrong, and wrong twice where the model was right.
/// </para>
/// </summary>
public static class ClassSuggestionPolicy
{
    /// <summary>Content as compared: trimmed, inner blanks collapsed to one space, lower-cased (invariant); null is empty.</summary>
    public static string Normalize(string? content) =>
        string.Join(' ', (content ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    /// <summary>Whether <paramref name="content"/> has a letter or a digit to suggest from.</summary>
    public static bool HasEvidence(string? content) => content != null && content.Any(char.IsLetterOrDigit);
}
