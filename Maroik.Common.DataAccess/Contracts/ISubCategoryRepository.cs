using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts;

public interface ISubCategoryRepository
{
    /// <summary>
    /// Gets all subcategories.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<SubCategory>> GetSubCategoryAsync();
    /// <summary>
    /// Gets subcategories by permission.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role);
    /// <summary>
    /// Creates a subcategory.
    /// </summary>
    /// <param name="subCategory"></param>
    /// <returns></returns>
    public Task CreateSubCategoryAsync(SubCategory subCategory);
    /// <summary>
    /// Updates a subcategory.
    /// </summary>
    /// <param name="subCategory"></param>
    /// <returns></returns>
    public Task UpdateSubCategoryAsync(SubCategory subCategory);
    /// <summary>
    /// Removes a subcategory.
    /// </summary>
    /// <param name="subCategory"></param>
    /// <returns></returns>
    public Task DeleteSubCategoryAsync(SubCategory subCategory);
}