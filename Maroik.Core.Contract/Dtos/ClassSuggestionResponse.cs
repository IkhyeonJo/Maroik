namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// The expenditure class proposed for a new record's content, learned from the account's own past expenditures. It is a
/// hint the create form shows; the user applies it by hand, and the record is validated on save like any other.
/// </summary>
public class ClassSuggestionResponse
{
    /// <summary>The proposed main class, as stored (e.g. "ConsumerSpending").</summary>
    public required string MainClass { get; init; }

    /// <summary>The proposed sub-class of that main class, as stored (e.g. "MealOrEatOutExpenses").</summary>
    public required string SubClass { get; init; }
}
