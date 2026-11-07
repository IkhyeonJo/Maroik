namespace Maroik.Core.Contract.Dtos;

/// <summary>One cell of a budget edit.</summary>
/// <param name="Address">The cell's A1 address.</param>
/// <param name="Input">The text typed into it, or null to clear it.</param>
public sealed record BudgetCellEditRequest(string Address, string? Input);
