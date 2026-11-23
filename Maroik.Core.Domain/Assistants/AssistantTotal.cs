namespace Maroik.Core.Domain.Assistants;

/// <summary>The sum of the records of one group (a class, or "all") in one currency.</summary>
/// <param name="Group">What the records share, e.g. a main class.</param>
/// <param name="Currency">The currency of every summed amount.</param>
/// <param name="Amount">The sum.</param>
/// <param name="Count">How many records were summed.</param>
public sealed record AssistantTotal(string Group, string Currency, decimal Amount, int Count);
