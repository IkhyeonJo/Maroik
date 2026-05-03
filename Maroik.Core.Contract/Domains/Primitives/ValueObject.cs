namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Base class for all value objects.
/// Value objects have no identity — equality is determined entirely by their atomic values.
/// Subclasses must implement <see cref="GetAtomicValues"/> to declare which fields compose the value.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>Returns the values that define equality for this value object.</summary>
    public abstract IEnumerable<object?> GetAtomicValues();

    /// <inheritdoc/>
    public bool Equals(ValueObject? other)
    {
        if (other is null || other.GetType() != GetType())
            return false;

        return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ValueObject vo && Equals(vo);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        GetAtomicValues().Aggregate(
            default(int),
            (hash, value) => HashCode.Combine(hash, value?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);
    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
