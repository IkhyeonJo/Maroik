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
    protected override DbSet<OrmCategory> Set => Context.Categories;

    protected override Category ToDomain(OrmCategory e)
        => Category.Reconstitute(e.Id, e.Name, e.DisplayName, e.IconPath, e.Controller, e.Action, e.Role, e.Order);

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

    public Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);

    public Task DeleteByIdAsync(int id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
