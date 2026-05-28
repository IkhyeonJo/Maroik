using Maroik.Core.Contract.Filters;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Generic repository contract for dynamic condition-based CRUD operations.
/// All repository interfaces extend this so callers can use a single FindAsync
/// instead of one per-condition method.
/// </summary>
public interface IGenericRepository<TDomain>
{
    /// <summary>
    /// Returns all domain objects that satisfy every condition in <paramref name="conditions"/>.
    /// An empty list means "no filter" (returns everything).
    /// </summary>
    Task<List<TDomain>> FindAsync(
        IReadOnlyList<FilterCondition> conditions,
        string? orderByProperty = null,
        bool descending = false);

    /// <summary>Returns a paged slice of matching records together with the total matching count.</summary>
    Task<(List<TDomain> Items, int TotalCount)> FindPagedAsync(
        IReadOnlyList<FilterCondition> conditions,
        int page,
        int pageSize,
        string? orderByProperty = null,
        bool descending = false);

    /// <summary>Inserts a new domain object.</summary>
    Task CreateAsync(TDomain domain);

    /// <summary>
    /// Applies partial column updates to all rows matching <paramref name="conditions"/>.
    /// Keys in <paramref name="updates"/> must match ORM entity property names.
    /// </summary>
    Task UpdateAsync(
        IReadOnlyList<FilterCondition> conditions,
        IReadOnlyDictionary<string, object?> updates);

    /// <summary>Hard-deletes all rows matching <paramref name="conditions"/>.</summary>
    Task DeleteAsync(IReadOnlyList<FilterCondition> conditions);

    /// <summary>
    /// Full-entity update: fetches the existing row by its primary key and overwrites every column.
    /// A no-op when no row with the same PK exists.
    /// </summary>
    Task UpdateEntityAsync(TDomain domain);
}
