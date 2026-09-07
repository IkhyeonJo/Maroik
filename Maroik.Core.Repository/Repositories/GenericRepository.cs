using System.Linq.Expressions;
using System.Reflection;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// Base class for EF Core repositories, implementing the create/update operations of
/// <see cref="IGenericRepository{TDomain}"/> and providing protected query/delete helpers shared
/// by every concrete repository.
/// </summary>
/// <typeparam name="TDomain">The domain aggregate/entity type exposed to the rest of the application.</typeparam>
/// <typeparam name="TEntity">The EF Core persistence model (table mapping) type in <see cref="Maroik.Core.PostgreSQL.Models"/>.</typeparam>
/// <remarks>
/// Subclasses must implement <see cref="Set"/>, <see cref="ToDomain"/>, and <see cref="ToEntity"/> to
/// wire up the concrete <see cref="DbSet{TEntity}"/> and the domain/entity mapping in both directions,
/// and may override <see cref="ApplyIncludes"/> to eager-load navigation properties needed by <see cref="ToDomain"/>.
/// <see cref="ApplicationDbContext"/> uses EF Core's default (tracking) query behavior — it does NOT
/// disable change tracking globally — so <see cref="UpdateEntityAsync"/> goes through explicit
/// <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry"/> state assignment (rather than
/// relying on auto-detect-changes against an entity mutated in place) simply because the freshly-mapped
/// entity built from the domain object is always a new, Detached instance, not because tracking itself
/// is off.
/// </remarks>
public abstract class GenericRepository<TDomain, TEntity>(ApplicationDbContext context) : IGenericRepository<TDomain>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context = context;

    /// <summary>The concrete <see cref="DbSet{TEntity}"/> on <see cref="ApplicationDbContext"/> this repository operates on.</summary>
    protected abstract DbSet<TEntity> Set { get; }

    /// <summary>Maps a persisted <typeparamref name="TEntity"/> row to the <typeparamref name="TDomain"/> domain object.</summary>
    protected abstract TDomain ToDomain(TEntity entity);

    /// <summary>Maps a <typeparamref name="TDomain"/> domain object to its <typeparamref name="TEntity"/> persistence representation.</summary>
    protected abstract TEntity ToEntity(TDomain domain);

    /// <summary>
    /// Hook for subclasses to eager-load navigation properties (e.g. via <c>.Include(...)</c>) that
    /// <see cref="ToDomain"/> needs. No-op by default.
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    /// <summary>
    /// Compiled getters for <typeparamref name="TEntity"/>'s primary-key properties, built once per
    /// closed generic type and reused for every <see cref="UpdateEntityAsync"/> /
    /// <see cref="DeleteWhereAsync"/> call. This replaces a per-call <see cref="PropertyInfo.GetValue"/>
    /// reflection walk; the key-property list still comes from the EF model (itself cached by EF Core),
    /// only the value extraction is compiled. Assigned via <c>??=</c> — a benign race just rebuilds an
    /// identical delegate array.
    /// </summary>
    private static Func<TEntity, object?>[]? _primaryKeyGetters;

    private Func<TEntity, object?>[] BuildPrimaryKeyGetters()
    {
        var keyProperties = Context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;

        return [.. keyProperties.Select(property =>
        {
            var entity = Expression.Parameter(typeof(TEntity), "e");
            Expression access = Expression.Property(entity, property.PropertyInfo!);
            return Expression.Lambda<Func<TEntity, object?>>(
                Expression.Convert(access, typeof(object)), entity).Compile();
        })];
    }

    /// <summary>Reads <paramref name="entity"/>'s primary-key values (simple or composite) in key order.</summary>
    private object?[] GetPrimaryKeyValues(TEntity entity) =>
        [.. (_primaryKeyGetters ??= BuildPrimaryKeyGetters()).Select(getter => getter(entity))];

    /// <inheritdoc />
    public async Task CreateAsync(TDomain domain, CancellationToken ct = default)
    {
        _ = await Set.AddAsync(ToEntity(domain), ct);
        _ = await Context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateEntityAsync(TDomain domain, CancellationToken ct = default)
    {
        var newEntity = ToEntity(domain);
        // ToEntity always builds a brand-new instance that this context has never seen, so it starts
        // out Detached regardless of the context's tracking mode. Read the primary key (works for
        // both simple and composite keys) via the compiled per-type getters, then re-find the row
        // via Set.FindAsync, which returns a tracked instance we can then overwrite.
        var keyValues = GetPrimaryKeyValues(newEntity);
        var existing = await Set.FindAsync(keyValues, ct);
        if (existing is null)
            throw new InvalidOperationException(
                $"Cannot update {typeof(TEntity).Name}: no row found for key ({string.Join(", ", keyValues)}). " +
                "It may have been deleted concurrently.");
        var entry = Context.Entry(existing);
        if (entry.State == EntityState.Detached) entry.State = EntityState.Unchanged;
        // Copy all scalar values from the mapped domain object onto the tracked entity, then mark
        // the whole entry Modified so SaveChanges issues an UPDATE for every column regardless of
        // which individual values actually changed.
        entry.CurrentValues.SetValues(newEntity);
        entry.State = EntityState.Modified;
        _ = await Context.SaveChangesAsync(ct);
    }

    /// <summary>Runs <paramref name="predicate"/> (with optional <paramref name="orderBy"/>) and maps every matching row to <typeparamref name="TDomain"/>.</summary>
    protected async Task<List<TDomain>> QueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        var query = ApplyIncludes(Set.Where(predicate));
        if (orderBy != null) query = orderBy(query);
        return [.. (await query.ToListAsync(ct)).Select(ToDomain)];
    }

    /// <summary>Returns every row in <see cref="Set"/> (with optional <paramref name="orderBy"/>), mapped to <typeparamref name="TDomain"/>.</summary>
    protected async Task<List<TDomain>> QueryAllAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        var query = ApplyIncludes(Set.AsQueryable());
        if (orderBy != null) query = orderBy(query);
        return [.. (await query.ToListAsync(ct)).Select(ToDomain)];
    }

    /// <summary>Returns the first row matching <paramref name="predicate"/> mapped to <typeparamref name="TDomain"/>, or <see langword="default"/> if none exists.</summary>
    protected async Task<TDomain?> QueryFirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var entity = await ApplyIncludes(Set.Where(predicate)).FirstOrDefaultAsync(ct);
        return entity == null ? default : ToDomain(entity);
    }

    /// <summary>Deletes every row matching <paramref name="predicate"/>.</summary>
    protected async Task DeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        // Fetch the matching rows once, explicitly AsNoTracking so this lookup never disturbs
        // entities already tracked in the current unit of work. Set.Remove requires a tracked
        // entity. If this context already has a tracked instance for a given row (e.g. it was
        // just added/queried earlier in the same unit of work), that instance is removed directly
        // — attaching the freshly-fetched no-tracking copy instead would conflict with it (same
        // key, two instances). Otherwise the fetched row is attached in memory (no DB round trip)
        // before being removed. Both paths avoid the DB round trip that a per-row Set.FindAsync
        // would incur.
        var toRemove = await Set.AsNoTracking().Where(predicate).ToListAsync(ct);

        foreach (var entity in toRemove)
        {
            var keyValues = GetPrimaryKeyValues(entity);
            var trackedEntry = Context.ChangeTracker.Entries<TEntity>()
                .FirstOrDefault(e => GetPrimaryKeyValues(e.Entity).SequenceEqual(keyValues));

            if (trackedEntry != null)
            {
                Set.Remove(trackedEntry.Entity);
            }
            else
            {
                Set.Attach(entity);
                Set.Remove(entity);
            }
        }

        _ = await Context.SaveChangesAsync(ct);
    }
}
