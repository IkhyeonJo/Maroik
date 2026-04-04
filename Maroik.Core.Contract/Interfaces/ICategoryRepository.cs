using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICategoryRepository
{
    public Task<IEnumerable<CategoryResponse>> GetCategoryAsync();
    public Task<IEnumerable<CategoryResponse>> GetCategoryByRoleAsync(string role);
    public Task CreateCategoryAsync(CategoryRequest request);
    public Task UpdateCategoryAsync(CategoryRequest request);
    public Task DeleteCategoryAsync(CategoryRequest request);
}
