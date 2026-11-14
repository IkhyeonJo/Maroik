namespace Maroik.Core.Domain.Finances;

/// <summary>A sub-class's spending in a month and its share of the month's total.</summary>
/// <param name="SubClass">The sub-class, as stored.</param>
/// <param name="Amount">Its spending in the month.</param>
/// <param name="Percentage">Its share of the month's total, in whole percent.</param>
public sealed record SubClassShare(string SubClass, decimal Amount, int Percentage);
