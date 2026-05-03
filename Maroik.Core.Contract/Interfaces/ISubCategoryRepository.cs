using Maroik.Core.Contract.Domains.Menu;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the SubCategory (sidebar dropdown menu item) table.
/// </summary>
public interface ISubCategoryRepository
{
    /// <summary>Returns all sub-category rows regardless of role.</summary>
    public Task<IEnumerable<SubCategory>> GetSubCategoryAsync();

    /// <summary>Returns only the sub-categories visible to the given role.</summary>
    public Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role);

    /// <summary>Inserts a new sub-category row.</summary>
    public Task CreateSubCategoryAsync(SubCategory subCategory);

    /// <summary>Updates an existing sub-category's metadata and display order.</summary>
    public Task UpdateSubCategoryAsync(SubCategory subCategory);

    /// <summary>Deletes a sub-category row.</summary>
    public Task DeleteSubCategoryAsync(SubCategory subCategory);
}
