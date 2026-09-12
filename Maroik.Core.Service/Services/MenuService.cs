using Maroik.Core.Domain.Menu;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IMenuService"/> that delegates navigation menu management
/// to <see cref="ICategoryRepository"/> and <see cref="ISubCategoryRepository"/>.
/// All write operations turn a caught exception into a <see cref="ServiceResult.Failure"/> and a
/// missing row into a <see cref="ServiceResult.NotFound"/>, so the controller can surface a
/// user-friendly error (and react to its kind) without exposing internal details.
/// </summary>
public class MenuService(
    ICategoryRepository categoryRepository,
    ISubCategoryRepository subCategoryRepository,
    ILogger<MenuService> logger) : IMenuService
{
    /// <inheritdoc />
    public async Task<IEnumerable<CategoryResponse>> GetAllCategoriesAsync(CancellationToken ct = default)
        => (await categoryRepository.GetAllAsync(ct)).Select(CategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetAllSubCategoriesAsync(CancellationToken ct = default)
        => (await subCategoryRepository.GetAllAsync(ct)).Select(SubCategoryMapper.ToResponse);

    /// <inheritdoc />
    public async Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        Category? category = await categoryRepository.FindByIdAsync(id, ct);
        return category == null ? null : CategoryMapper.ToResponse(category);
    }

    /// <inheritdoc />
    public async Task<SubCategoryResponse?> GetSubCategoryByIdAsync(int id, CancellationToken ct = default)
    {
        SubCategory? sub = await subCategoryRepository.FindByIdAsync(id, ct);
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
                return ServiceResult.FromError(result.FirstError);

            await categoryRepository.CreateAsync(result.Value, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create category {Name}", request.Name);
            return ServiceResult.Failure("Menu.CreateCategoryFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            Category? category = await categoryRepository.FindByIdAsync(request.Id, ct);
            if (category == null)
                return ServiceResult.NotFound("Category.NotFound", "Input is invalid");

            var updateResult = category.Update(request.Name, request.DisplayName, request.IconPath,
                request.Controller, request.Action, request.Role, request.Order);
            if (updateResult.IsError)
                return ServiceResult.FromError(updateResult.FirstError);

            await categoryRepository.UpdateEntityAsync(category, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update category {Name}", request.Name);
            return ServiceResult.Failure("Menu.UpdateCategoryFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            await categoryRepository.DeleteByIdAsync(request.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete category {Name}", request.Name);
            return ServiceResult.Failure("Menu.DeleteCategoryFailed", "Input is invalid");
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
                return ServiceResult.FromError(result.FirstError);

            await subCategoryRepository.CreateAsync(result.Value, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to create sub-category {Name}", request.Name);
            return ServiceResult.Failure("Menu.CreateSubCategoryFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            SubCategory? subCategory = await subCategoryRepository.FindByIdAsync(request.Id, ct);
            if (subCategory == null)
                return ServiceResult.NotFound("SubCategory.NotFound", "Input is invalid");

            var updateResult = subCategory.Update(request.CategoryId, request.Name,
                request.DisplayName, request.IconPath, request.Action, request.Role, request.Order);
            if (updateResult.IsError)
                return ServiceResult.FromError(updateResult.FirstError);

            await subCategoryRepository.UpdateEntityAsync(subCategory, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update sub-category {Name}", request.Name);
            return ServiceResult.Failure("Menu.UpdateSubCategoryFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request, CancellationToken ct = default)
    {
        try
        {
            await subCategoryRepository.DeleteByIdAsync(request.Id, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to delete sub-category {Name}", request.Name);
            return ServiceResult.Failure("Menu.DeleteSubCategoryFailed", "Input is invalid");
        }
    }
}
