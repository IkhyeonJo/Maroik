namespace Maroik.Core.Contract.Dtos;

/// <summary>What the budget page needs to know about one sheet kind; authoritative in the Domain.</summary>
public sealed class BudgetPageRules
{
    /// <summary>The main classes, in display order.</summary>
    public IReadOnlyList<string> MainClasses { get; init; } = [];

    /// <summary>The subclasses of each main class, in display order.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> SubClassesByMainClass { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>The month (1 to 12) each month column holds, in column order.</summary>
    public IReadOnlyDictionary<string, int> MonthByColumn { get; init; } = new Dictionary<string, int>();

    /// <summary>The column holding the year total.</summary>
    public string YearColumn { get; init; } = "";

    /// <summary>The functions the formula editor offers, in the order it lists them.</summary>
    public IReadOnlyList<BudgetFunctionResponse> Functions { get; init; } = [];
}
