using Maroik.Core.Contract.Domains.Menu;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Category (top-level navigation menu) table.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>Returns all navigation categories regardless of role.</summary>
    public Task<IEnumerable<Category>> GetCategoryAsync();

    /// <summary>Returns only the categories visible to the given role (e.g. "Admin", "User").</summary>
    public Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role);

    /// <summary>Inserts a new category row.</summary>
    public Task CreateCategoryAsync(Category category);

    /// <summary>Updates an existing category's metadata and display order.</summary>
    public Task UpdateCategoryAsync(Category category);

    /// <summary>Deletes a category row (and cascades to its sub-categories via the DB constraint).</summary>
    public Task DeleteCategoryAsync(Category category);
}
