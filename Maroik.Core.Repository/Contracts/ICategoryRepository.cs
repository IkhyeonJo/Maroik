using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface ICategoryRepository
{
    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<Category>> GetCategoryAsync();
    /// <summary>
    /// Gets categories by permission.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role);
    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public Task CreateCategoryAsync(Category category);
    /// <summary>
    /// Updates a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public Task UpdateCategoryAsync(Category category);
    /// <summary>
    /// Removes a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public Task DeleteCategoryAsync(Category category);
}