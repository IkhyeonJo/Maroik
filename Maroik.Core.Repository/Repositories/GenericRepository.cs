using Maroik.Core.Contract.Filters;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// Abstract base providing generic EF Core implementations of <see cref="IGenericRepository{TDomain}"/>.
/// Concrete repositories extend this class, implement <see cref="Set"/>, <see cref="ToDomain"/>,
/// and <see cref="ToEntity"/>, and may override <see cref="ApplyIncludes"/> for eager loading.
/// </summary>
public abstract class GenericRepository<TDomain, TEntity> : IGenericRepository<TDomain>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context;

    protected GenericRepository(ApplicationDbContext context) => Context = context;

    protected abstract DbSet<TEntity> Set { get; }
    protected abstract TDomain ToDomain(TEntity entity);
    protected abstract TEntity ToEntity(TDomain domain);

    /// <summary>
    /// Override in derived classes to add <c>Include</c> chains that must be present
    /// on every query executed through this repository.
    /// </summary>
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    /// <inheritdoc />
    public async Task<List<TDomain>> FindAsync(
        IReadOnlyList<FilterCondition> conditions,
        string? orderByProperty = null,
        bool descending = false)
    {
        var query = ApplyIncludes(Set.Where(FilterExpressionBuilder.Build<TEntity>(conditions)));
        if (orderByProperty is not null)
            query = FilterExpressionBuilder.ApplyOrderBy(query, orderByProperty, descending);
        return (await query.ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<(List<TDomain> Items, int TotalCount)> FindPagedAsync(
        IReadOnlyList<FilterCondition> conditions,
        int page,
        int pageSize,
        string? orderByProperty = null,
        bool descending = false)
    {
        var query = ApplyIncludes(Set.Where(FilterExpressionBuilder.Build<TEntity>(conditions)));
        if (orderByProperty is not null)
            query = FilterExpressionBuilder.ApplyOrderBy(query, orderByProperty, descending);
        int total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(ToDomain).ToList(), total);
    }

    /// <inheritdoc />
    public async Task CreateAsync(TDomain domain)
    {
        _ = await Set.AddAsync(ToEntity(domain));
        _ = await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        IReadOnlyList<FilterCondition> conditions,
        IReadOnlyDictionary<string, object?> updates)
    {
        var entities = await Set
            .Where(FilterExpressionBuilder.Build<TEntity>(conditions))
            .ToListAsync();

        foreach (var entity in entities)
        {
            foreach (var (key, value) in updates)
                Context.Entry(entity).Property(key).CurrentValue = value;
        }

        _ = await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteAsync(IReadOnlyList<FilterCondition> conditions)
    {
        // Load PKs without tracking to avoid conflicts with already-tracked instances.
        // FindAsync then resolves to the identity-map instance (or re-attaches as needed)
        // before marking Deleted ?? this works correctly with both InMemory and PostgreSQL.
        var toRemove = await Set.AsNoTracking()
            .Where(FilterExpressionBuilder.Build<TEntity>(conditions))
            .ToListAsync();

        var pk = Context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;
        foreach (var entity in toRemove)
        {
            var keyValues = pk.Select(p => p.PropertyInfo!.GetValue(entity)).ToArray();
            var tracked = await Set.FindAsync(keyValues);
            if (tracked is not null)
                Set.Remove(tracked);
        }

        _ = await Context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateEntityAsync(TDomain domain)
    {
        var newEntity = ToEntity(domain);
        var pk = Context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;
        var keyValues = pk.Select(p => p.PropertyInfo!.GetValue(newEntity)).ToArray();

        var existing = await Set.FindAsync(keyValues);
        if (existing is null) return;

        var entry = Context.Entry(existing);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Unchanged;

        entry.CurrentValues.SetValues(newEntity);
        // Explicit Modified ensures SaveChanges emits UPDATE even when AutoDetectChanges is off.
        entry.State = EntityState.Modified;

        _ = await Context.SaveChangesAsync();
    }
}
