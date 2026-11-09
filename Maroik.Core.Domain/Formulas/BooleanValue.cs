namespace Maroik.Core.Domain.Formulas;

/// <summary>TRUE or FALSE.</summary>
/// <param name="Value">The boolean.</param>
public sealed record BooleanValue(bool Value) : FormulaValue
{
    /// <summary>The value as a sheet shows it.</summary>
    public string Text => Value ? "TRUE" : "FALSE";
}
