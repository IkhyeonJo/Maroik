using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ISubCategoryRepository
{
    public Task<IEnumerable<SubCategoryResponse>> GetSubCategoryAsync();
    public Task<IEnumerable<SubCategoryResponse>> GetSubCategoryByRoleAsync(string role);
    public Task CreateSubCategoryAsync(SubCategoryRequest request);
    public Task UpdateSubCategoryAsync(SubCategoryRequest request);
    public Task DeleteSubCategoryAsync(SubCategoryRequest request);
}
