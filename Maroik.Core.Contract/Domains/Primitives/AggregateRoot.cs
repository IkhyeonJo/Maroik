namespace Maroik.Core.Contract.Domains.Primitives;

/// <summary>Base class for all aggregate roots.</summary>
/// <typeparam name="TId">Type of the aggregate's primary key.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    protected AggregateRoot(TId id) : base(id) { }
}
