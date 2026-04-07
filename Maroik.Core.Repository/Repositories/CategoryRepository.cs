using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICategoryRepository"/>.</summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategoryResponse>> GetCategoryAsync()
    {
        return (await _context.Categories.ToListAsync()).Adapt<List<CategoryResponse>>();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CategoryResponse>> GetCategoryByRoleAsync(string role)
    {
        return (await _context.Categories.Where(a => a.Role == role).OrderBy(a => a.Order).ToListAsync()).Adapt<List<CategoryResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateCategoryAsync(CategoryRequest request)
    {
        _ = await _context.Categories.AddAsync(request.Adapt<Category>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCategoryAsync(CategoryRequest request)
    {
        _ = _context.Categories.Update(request.Adapt<Category>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCategoryAsync(CategoryRequest request)
    {
        _ = _context.Categories.Remove(request.Adapt<Category>());
        _ = await _context.SaveChangesAsync();
    }
}
