using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCategory = Maroik.Core.PostgreSQL.Models.Category;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICategoryRepository"/>.</summary>
public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetCategoryAsync()
        => (await context.Categories.ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role)
        => (await context.Categories.Where(c => c.Role == role).OrderBy(c => c.Order).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task CreateCategoryAsync(Category category)
    {
        _ = await context.Categories.AddAsync(ToEntity(category));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCategoryAsync(Category category)
    {
        _ = context.Categories.Update(ToEntity(category));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(Category category)
    {
        _ = context.Categories.Remove(ToEntity(category));
        _ = await context.SaveChangesAsync();
    }

    private static Category ToDomain(OrmCategory e)
        => Category.Reconstitute(e.Id, e.Name, e.DisplayName, e.IconPath, e.Controller, e.Action, e.Role, e.Order);

    private static OrmCategory ToEntity(Category c) => new()
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
}
