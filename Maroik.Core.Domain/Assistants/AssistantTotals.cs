namespace Maroik.Core.Domain.Assistants;

/// <summary>
/// Adds up the amounts the AI chat reports. The server sums them, never the model (a model's arithmetic is not to be trusted), and
/// amounts of different currencies are never added together.
/// </summary>
public static class AssistantTotals
{
    /// <summary>The sum and count per group and currency, ordered by group then currency.</summary>
    public static IReadOnlyList<AssistantTotal> Sum(IEnumerable<(string Group, string Currency, decimal Amount)> items) =>
    [
        .. items
            .GroupBy(item => (item.Group, item.Currency))
            .Select(group => new AssistantTotal(group.Key.Group, group.Key.Currency, group.Sum(item => item.Amount), group.Count()))
            .OrderBy(total => total.Group, StringComparer.Ordinal)
            .ThenBy(total => total.Currency, StringComparer.Ordinal)
    ];
}
