using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IMenuService"/> that delegates navigation menu management
/// to <see cref="ICategoryRepository"/> and <see cref="ISubCategoryRepository"/>.
/// All write operations wrap exceptions in a <see cref="ServiceResult.Fail"/> so the
/// controller can surface a user-friendly error without exposing internal details.
/// </summary>
public class MenuService : IMenuService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly ILogger<MenuService> _logger;

    /// <summary>Initializes a new instance of <see cref="MenuService"/> with the supplied dependencies.</summary>
    public MenuService(
        ICategoryRepository categoryRepository,
        ISubCategoryRepository subCategoryRepository,
        ILogger<MenuService> logger)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync()
        => (await _categoryRepository.GetCategoryAsync()).Select(CategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync()
        => (await _subCategoryRepository.GetSubCategoryAsync()).Select(SubCategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
    {
        Category? category = (await _categoryRepository.GetCategoryAsync()).FirstOrDefault(c => c.Id == id);
        return category == null ? null : CategoryMapper.ToResponse(category);
    }

    /// <inheritdoc />
    public async Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id)
    {
        SubCategory? sub = (await _subCategoryRepository.GetSubCategoryAsync()).FirstOrDefault(s => s.Id == id);
        return sub == null ? null : SubCategoryMapper.ToResponse(sub);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCategoryAsync(CategoryRequest request)
    {
        try
        {
            var result = Category.Create(0, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            if (result.IsError)
                return ServiceResult.Fail(result.FirstError.Description);

            await _categoryRepository.CreateCategoryAsync(result.Value);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request)
    {
        try
        {
            var category = Category.Reconstitute(request.Id, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            await _categoryRepository.UpdateCategoryAsync(category);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request)
    {
        try
        {
            var category = Category.Reconstitute(request.Id, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            await _categoryRepository.DeleteCategoryAsync(category);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            var result = SubCategory.Create(0, request.CategoryId, request.Name, request.DisplayName,
                request.IconPath, request.Action, request.Role, request.Order);
            if (result.IsError)
                return ServiceResult.Fail(result.FirstError.Description);

            await _subCategoryRepository.CreateSubCategoryAsync(result.Value);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            var subCategory = SubCategory.Reconstitute(request.Id, request.CategoryId, request.Name,
                request.DisplayName, request.IconPath, request.Action, request.Role, request.Order);
            await _subCategoryRepository.UpdateSubCategoryAsync(subCategory);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            var subCategory = SubCategory.Reconstitute(request.Id, request.CategoryId, request.Name,
                request.DisplayName, request.IconPath, request.Action, request.Role, request.Order);
            await _subCategoryRepository.DeleteSubCategoryAsync(subCategory);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
