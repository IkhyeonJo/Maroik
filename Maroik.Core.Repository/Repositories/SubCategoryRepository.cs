using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmSubCategory = Maroik.Core.PostgreSQL.Models.SubCategory;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ISubCategoryRepository"/>.</summary>
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
        _ = context.SubCategories.Update(ToEntity(subCategory));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteSubCategoryAsync(SubCategory subCategory)
    {
        _ = context.SubCategories.Remove(ToEntity(subCategory));
        _ = await context.SaveChangesAsync();
    }

    private static SubCategory ToDomain(OrmSubCategory e)
        => SubCategory.Reconstitute(e.Id, e.CategoryId, e.Name, e.DisplayName, e.IconPath, e.Action, e.Role, e.Order);

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
