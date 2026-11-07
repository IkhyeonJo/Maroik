namespace Maroik.Core.Contract.Dtos;

/// <summary>A function the budget formula editor offers.</summary>
/// <param name="Name">Its name, e.g. <c>SUM</c>.</param>
/// <param name="MinArguments">The fewest arguments it takes.</param>
/// <param name="MaxArguments">The most arguments it takes.</param>
/// <param name="Signature">Its signature as the editor shows it.</param>
public sealed record BudgetFunctionResponse(string Name, int MinArguments, int MaxArguments, string Signature);
