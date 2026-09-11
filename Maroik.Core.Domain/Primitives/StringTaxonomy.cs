namespace Maroik.Core.Domain.Primitives;

/// <summary>
/// Shared "closed set of known string values" primitive backing every DB-CHECK-mirrored taxonomy
/// in the domain (board type, calendar event status, reminder method, asset item, ...). Each
/// taxonomy class declares only its own constants (or, for an enum-backed taxonomy, its enum) and
/// delegates the <c>All</c> / <c>IsKnown</c> membership check to one instance of this type, instead
/// of every taxonomy hand-rolling its own <see cref="HashSet{T}"/> and null-checked lookup.
/// </summary>
public sealed class StringTaxonomy
{
    private readonly IReadOnlySet<string> _values;

    /// <summary>Builds the taxonomy from its complete set of recognised values.</summary>
    public StringTaxonomy(params IEnumerable<string> values) =>
        _values = new HashSet<string>(values, StringComparer.Ordinal);

    /// <summary>Every recognised value.</summary>
    public IReadOnlySet<string> All => _values;

    /// <summary>Returns true when <paramref name="value"/> is one of the known values.</summary>
    public bool IsKnown(string? value) => value != null && _values.Contains(value);
}
