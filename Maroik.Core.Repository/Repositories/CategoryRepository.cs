using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCategory = Maroik.Core.PostgreSQL.Models.Category;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Category"/> domain objects,
/// representing top-level navigation menu categories.
/// </summary>
public class CategoryRepository(ApplicationDbContext context)
    : GenericRepository<Category, OrmCategory>(context), ICategoryRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmCategory"/> rows.</summary>
    protected override DbSet<OrmCategory> Set => Context.Categories;

    /// <summary>Maps a persisted <see cref="OrmCategory"/> row to the <see cref="Category"/> domain object.</summary>
    protected override Category ToDomain(OrmCategory e)
        => Category.Reconstitute(e.Id, e.Name, e.DisplayName, e.IconPath, e.Controller, e.Action, e.Role, e.Order);

    /// <summary>Maps a <see cref="Category"/> domain object to its <see cref="OrmCategory"/> persistence representation.</summary>
    protected override OrmCategory ToEntity(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        DisplayName = c.DisplayName,
        IconPath = c.IconPath,
        Controller = c.Controller,
        Action = c.Action,
        Role = c.Role,
        Order = c.Order
    };

    /// <inheritdoc />
    public Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
