using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
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
        => await _categoryRepository.GetCategoryAsync();

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync()
        => await _subCategoryRepository.GetSubCategoryAsync();

    /// <inheritdoc />
    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
    {
        IEnumerable<CategoryResponse> categories = await _categoryRepository.GetCategoryAsync();
        return categories.FirstOrDefault(c => c.Id == id);
    }

    /// <inheritdoc />
    public async Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id)
    {
        IEnumerable<SubCategoryResponse> subCategories = await _subCategoryRepository.GetSubCategoryAsync();
        return subCategories.FirstOrDefault(s => s.Id == id);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCategoryAsync(CategoryRequest request)
    {
        try
        {
            await _categoryRepository.CreateCategoryAsync(request);
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
            await _categoryRepository.UpdateCategoryAsync(request);
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
            await _categoryRepository.DeleteCategoryAsync(request);
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
            await _subCategoryRepository.CreateSubCategoryAsync(request);
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
            await _subCategoryRepository.UpdateSubCategoryAsync(request);
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
            await _subCategoryRepository.DeleteSubCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
