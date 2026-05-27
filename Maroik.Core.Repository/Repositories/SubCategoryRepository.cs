using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmSubCategory = Maroik.Core.PostgreSQL.Models.SubCategory;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISubCategoryRepository"/> that persists and retrieves
/// <see cref="SubCategory"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class SubCategoryRepository(ApplicationDbContext context) : ISubCategoryRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<SubCategory>> GetSubCategoryAsync()
        => (await context.SubCategories.ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role)
        => (await context.SubCategories.Where(s => s.Role == role).OrderBy(s => s.Order).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task CreateSubCategoryAsync(SubCategory subCategory)
    {
        _ = await context.SubCategories.AddAsync(ToEntity(subCategory));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateSubCategoryAsync(SubCategory subCategory)
    {
        var existing = await context.SubCategories.FindAsync(subCategory.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(subCategory));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteSubCategoryAsync(SubCategory subCategory)
    {
        var existing = await context.SubCategories.FindAsync(subCategory.Id);
        if (existing == null) return;
        _ = context.SubCategories.Remove(existing);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmSubCategory"/> row to the <see cref="SubCategory"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="SubCategory"/> domain object.</returns>
    private static SubCategory ToDomain(OrmSubCategory e)
        => SubCategory.Reconstitute(e.Id, e.CategoryId, e.Name, e.DisplayName, e.IconPath, e.Action, e.Role, e.Order);

    /// <summary>
    /// Maps a <see cref="SubCategory"/> domain object to an ORM <see cref="OrmSubCategory"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="s">The domain object to convert.</param>
    /// <returns>An <see cref="OrmSubCategory"/> entity suitable for EF Core tracking.</returns>
    private static OrmSubCategory ToEntity(SubCategory s) => new()
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
}
