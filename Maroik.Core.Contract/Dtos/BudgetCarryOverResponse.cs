namespace Maroik.Core.Contract.Dtos;

/// <summary>The outcome of carrying last year's inputs over: the sheet as it is now and how many cells were filled.</summary>
public sealed class BudgetCarryOverResponse
{
    /// <summary>The sheet after the carry-over.</summary>
    public required BudgetSheetResponse Sheet { get; init; }

    /// <summary>How many cells were filled from last year.</summary>
    public int Copied { get; init; }
}
