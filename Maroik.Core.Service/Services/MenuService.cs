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
    public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync(CancellationToken ct = default)
        => (await _categoryRepository.GetAllAsync(ct)).Select(CategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync(CancellationToken ct = default)
        => (await _subCategoryRepository.GetAllAsync(ct)).Select(SubCategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        Category? category = (await _categoryRepository.GetAllAsync(ct)).FirstOrDefault(c => c.Id == id);
        return category == null ? null : CategoryMapper.ToResponse(category);
    }

    /// <inheritdoc />
    public async Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        SubCategory? sub = (await _subCategoryRepository.GetAllAsync(ct)).FirstOrDefault(s => s.Id == id);
        return sub == null ? null : SubCategoryMapper.ToResponse(sub);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateCategoryAsync(CategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = Category.Create(0, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            if (result.IsError)
                return ServiceResult.Fail(result.FirstError.Description);

            await _categoryRepository.CreateAsync(result.Value, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            // GenericRepository.UpdateEntityAsync silently no-ops when the row doesn't exist
            // (it looks the row up and returns early rather than throwing), so without this check
            // an unknown Id would report "successfully updated" while changing nothing.
            bool exists = (await _categoryRepository.GetAllAsync(ct)).Any(c => c.Id == request.Id);
            if (!exists)
                return ServiceResult.Fail("Input is invalid");

            var category = Category.Reconstitute(request.Id, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            await _categoryRepository.UpdateEntityAsync(category, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var category = Category.Reconstitute(request.Id, request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            await _categoryRepository.DeleteByIdAsync(category.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = SubCategory.Create(0, request.CategoryId, request.Name, request.DisplayName,
                request.IconPath, request.Action, request.Role, request.Order);
            if (result.IsError)
                return ServiceResult.Fail(result.FirstError.Description);

            await _subCategoryRepository.CreateAsync(result.Value, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            // See UpdateCategoryAsync: without this check, an unknown Id would silently no-op
            // yet still report success.
            bool exists = (await _subCategoryRepository.GetAllAsync(ct)).Any(s => s.Id == request.Id);
            if (!exists)
                return ServiceResult.Fail("Input is invalid");

            var subCategory = SubCategory.Reconstitute(request.Id, request.CategoryId, request.Name,
                request.DisplayName, request.IconPath, request.Action, request.Role, request.Order);
            await _subCategoryRepository.UpdateEntityAsync(subCategory, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            var subCategory = SubCategory.Reconstitute(request.Id, request.CategoryId, request.Name,
                request.DisplayName, request.IconPath, request.Action, request.Role, request.Order);
            await _subCategoryRepository.DeleteByIdAsync(subCategory.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete sub-category {Name}", request.Name);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
