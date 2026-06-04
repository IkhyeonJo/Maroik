using System.Linq.Expressions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public abstract class GenericRepository<TDomain, TEntity> : IGenericRepository<TDomain>
    where TEntity : class
{
    protected readonly ApplicationDbContext Context;
    protected GenericRepository(ApplicationDbContext context) => Context = context;

    protected abstract DbSet<TEntity> Set { get; }
    protected abstract TDomain ToDomain(TEntity entity);
    protected abstract TEntity ToEntity(TDomain domain);
    protected virtual IQueryable<TEntity> ApplyIncludes(IQueryable<TEntity> query) => query;

    public async Task CreateAsync(TDomain domain, CancellationToken ct = default)
    {
        _ = await Set.AddAsync(ToEntity(domain), ct);
        _ = await Context.SaveChangesAsync(ct);
    }

    public async Task UpdateEntityAsync(TDomain domain, CancellationToken ct = default)
    {
        var newEntity = ToEntity(domain);
        var pk = Context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;
        var keyValues = pk.Select(p => p.PropertyInfo!.GetValue(newEntity)).ToArray();
        var existing = await Set.FindAsync(keyValues, ct);
        if (existing is null) return;
        var entry = Context.Entry(existing);
        if (entry.State == EntityState.Detached) entry.State = EntityState.Unchanged;
        entry.CurrentValues.SetValues(newEntity);
        entry.State = EntityState.Modified;
        _ = await Context.SaveChangesAsync(ct);
    }

    protected async Task<List<TDomain>> QueryAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        var query = ApplyIncludes(Set.Where(predicate));
        if (orderBy != null) query = orderBy(query);
        return (await query.ToListAsync(ct)).Select(ToDomain).ToList();
    }

    protected async Task<List<TDomain>> QueryAllAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? orderBy = null,
        CancellationToken ct = default)
    {
        var query = ApplyIncludes(Set.AsQueryable());
        if (orderBy != null) query = orderBy(query);
        return (await query.ToListAsync(ct)).Select(ToDomain).ToList();
    }

    protected async Task<TDomain?> QueryFirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var entity = await ApplyIncludes(Set.Where(predicate)).FirstOrDefaultAsync(ct);
        return entity == null ? default : ToDomain(entity);
    }

    protected async Task DeleteWhereAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
    {
        var toRemove = await Set.AsNoTracking().Where(predicate).ToListAsync(ct);
        var pk = Context.Model.FindEntityType(typeof(TEntity))!.FindPrimaryKey()!.Properties;
        foreach (var entity in toRemove)
        {
            var keyValues = pk.Select(p => p.PropertyInfo!.GetValue(entity)).ToArray();
            var tracked = await Set.FindAsync(keyValues, ct);
            if (tracked is not null) Set.Remove(tracked);
        }
        _ = await Context.SaveChangesAsync(ct);
    }
}
