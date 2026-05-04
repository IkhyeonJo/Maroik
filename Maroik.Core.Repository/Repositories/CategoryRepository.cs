using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCategory = Maroik.Core.PostgreSQL.Models.Category;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICategoryRepository"/> that persists and retrieves
/// <see cref="Category"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // Hard-delete: the category row is physically removed from the database.
        _ = context.Categories.Remove(ToEntity(category));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmCategory"/> row to the <see cref="Category"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Category"/> domain object.</returns>
    private static Category ToDomain(OrmCategory e)
        => Category.Reconstitute(e.Id, e.Name, e.DisplayName, e.IconPath, e.Controller, e.Action, e.Role, e.Order);

    /// <summary>
    /// Maps a <see cref="Category"/> domain object to an ORM <see cref="OrmCategory"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="c">The domain object to convert.</param>
    /// <returns>An <see cref="OrmCategory"/> entity suitable for EF Core tracking.</returns>
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
