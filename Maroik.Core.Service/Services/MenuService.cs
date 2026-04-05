using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

public class MenuService : IMenuService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly ILogger<MenuService> _logger;

    public MenuService(
        ICategoryRepository categoryRepository,
        ISubCategoryRepository subCategoryRepository,
        ILogger<MenuService> logger)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _logger = logger;
    }

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
