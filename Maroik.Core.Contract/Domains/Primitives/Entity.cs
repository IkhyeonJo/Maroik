namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>
/// Base class for all domain entities.
/// Entities have a persistent identity — equality is determined by <typeparamref name="TId"/>, not by field values.
/// </summary>
/// <typeparam name="TId">Type of the entity's primary key.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    /// <summary>Primary key that uniquely identifies this entity.</summary>
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    /// <inheritdoc/>
    public bool Equals(Entity<TId>? other) =>
        other is not null && GetType() == other.GetType() && Id.Equals(other.Id);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);
}
