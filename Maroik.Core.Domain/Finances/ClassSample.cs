namespace Maroik.Core.Domain.Finances;

/// <summary>
/// One past income or expenditure a class suggestion learns from (<see cref="IncomeClassSuggestionPolicy.Learnable"/> /
/// <see cref="ExpenditureClassSuggestionPolicy.Learnable"/>): its content as the model reads it
/// (<see cref="ClassSuggestionPolicy.Normalize"/>) and the valid class it was recorded under.
/// </summary>
/// <param name="Text">The normalized content.</param>
/// <param name="Class">The class it was recorded under (always a valid taxonomy pair).</param>
public sealed record ClassSample(string Text, ClassSuggestion Class);
