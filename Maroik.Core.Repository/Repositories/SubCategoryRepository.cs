using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ISubCategoryRepository"/>.</summary>
public class SubCategoryRepository : ISubCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public SubCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetSubCategoryAsync()
    {
        return (await _context.SubCategories.ToListAsync()).Adapt<List<SubCategoryResponse>>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SubCategoryResponse>> GetSubCategoryByRoleAsync(string role)
    {
        return (await _context.SubCategories.Where(a => a.Role == role).OrderBy(a => a.Order).ToListAsync()).Adapt<List<SubCategoryResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateSubCategoryAsync(SubCategoryRequest request)
    {
        _ = await _context.SubCategories.AddAsync(request.Adapt<SubCategory>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateSubCategoryAsync(SubCategoryRequest request)
    {
        _ = _context.SubCategories.Update(request.Adapt<SubCategory>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteSubCategoryAsync(SubCategoryRequest request)
    {
        _ = _context.SubCategories.Remove(request.Adapt<SubCategory>());
        _ = await _context.SaveChangesAsync();
    }
}
