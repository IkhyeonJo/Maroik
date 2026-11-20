namespace Maroik.Core.Domain.Finances;

/// <summary>
/// The class proposed for a new income or expenditure from its content: a valid pair of that book's taxonomy
/// (<see cref="IncomeClassPolicy"/> / <see cref="ExpenditureClassPolicy"/>), as the strings stored in the database. It is only a
/// hint — the user applies it by hand, and the record is validated on save like any other.
/// </summary>
/// <param name="MainClass">The proposed main class.</param>
/// <param name="SubClass">The proposed sub-class of that main class.</param>
public sealed record ClassSuggestion(string MainClass, string SubClass);
