using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for managing the sidebar navigation menu (categories and sub-categories).
/// Used by the admin Management > Menu page.
/// </summary>
public interface IMenuService
{
    /// <summary>Returns all top-level navigation categories.</summary>
    Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync();

    /// <summary>Returns all sub-category (dropdown menu item) rows.</summary>
    Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync();

    /// <summary>Returns a single category by its ID, or null.</summary>
    Task<CategoryResponse?> GetCategoryByIdAsync(int id);

    /// <summary>Returns a single sub-category by its ID, or null.</summary>
    Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id);

    /// <summary>Creates a new top-level navigation category.</summary>
    Task<ServiceResult> CreateCategoryAsync(CategoryRequest request);

    /// <summary>Updates an existing navigation category.</summary>
    Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request);

    /// <summary>Deletes a navigation category and its sub-categories.</summary>
    Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request);

    /// <summary>Creates a new sub-category under an existing category.</summary>
    Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request);

    /// <summary>Updates an existing sub-category.</summary>
    Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request);

    /// <summary>Deletes a sub-category.</summary>
    Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request);
}
