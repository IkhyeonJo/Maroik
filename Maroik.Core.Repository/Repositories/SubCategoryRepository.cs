using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmSubCategory = Maroik.Core.PostgreSQL.Models.SubCategory;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="SubCategory"/> domain objects,
/// representing child navigation menu items under a <see cref="Category"/>.
/// </summary>
public class SubCategoryRepository(ApplicationDbContext context)
    : GenericRepository<SubCategory, OrmSubCategory>(context), ISubCategoryRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmSubCategory"/> rows.</summary>
    protected override DbSet<OrmSubCategory> Set => Context.SubCategories;

    /// <summary>Maps a persisted <see cref="OrmSubCategory"/> row to the <see cref="SubCategory"/> domain object.</summary>
    protected override SubCategory ToDomain(OrmSubCategory e)
        => SubCategory.Reconstitute(e.Id, e.CategoryId, e.Name, e.DisplayName, e.IconPath, e.Action, e.Role, e.Order);

    /// <summary>Maps a <see cref="SubCategory"/> domain object to its <see cref="OrmSubCategory"/> persistence representation.</summary>
    protected override OrmSubCategory ToEntity(SubCategory s) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        Name = s.Name,
        DisplayName = s.DisplayName,
        IconPath = s.IconPath,
        Action = s.Action,
        Role = s.Role,
        Order = s.Order
    };

    /// <inheritdoc />
    public Task<List<SubCategory>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
