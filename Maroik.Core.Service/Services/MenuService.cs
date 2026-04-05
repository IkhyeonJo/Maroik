using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

public class MenuService : IMenuService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;

    public MenuService(ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
    {
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
    }

    public async Task<ServiceResult> CreateCategoryAsync(CategoryRequest request)
    {
        try
        {
            await _categoryRepository.CreateCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    public async Task<ServiceResult> UpdateCategoryAsync(CategoryRequest request)
    {
        try
        {
            await _categoryRepository.UpdateCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    public async Task<ServiceResult> DeleteCategoryAsync(CategoryRequest request)
    {
        try
        {
            await _categoryRepository.DeleteCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    public async Task<ServiceResult> CreateSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            await _subCategoryRepository.CreateSubCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    public async Task<ServiceResult> UpdateSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            await _subCategoryRepository.UpdateSubCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }

    public async Task<ServiceResult> DeleteSubCategoryAsync(SubCategoryRequest request)
    {
        try
        {
            await _subCategoryRepository.DeleteSubCategoryAsync(request);
            return ServiceResult.Ok();
        }
        catch { return ServiceResult.Fail("Input is invalid"); }
    }
}
