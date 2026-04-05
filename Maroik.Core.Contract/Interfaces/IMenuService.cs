using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IMenuService
{
    Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync();
    Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync();
    Task<CategoryResponse?> GetCategoryByIdAsync(int id);
    Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id);
    Task<ServiceResult> CreateCategoryAsync(CategoryRequest request);
    Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request);
    Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request);
    Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request);
    Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request);
    Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request);
}
