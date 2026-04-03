using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all categories.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Category>> GetCategoryAsync()
    {
        return await _context.Categories.ToListAsync();
    }
    /// <summary>
    /// Gets categories by permission.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role)
    {
        return await _context.Categories
                .Where(a => a.Role == role)
                .OrderBy(a => a.Order)
                .ToListAsync();
    }
    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public async Task CreateCategoryAsync(Category category)
    {
        _ = await _context.Categories.AddAsync(category);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Updates a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public async Task UpdateCategoryAsync(Category category)
    {
        _ = _context.Categories.Update(category);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Removes a category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public async Task DeleteCategoryAsync(Category category)
    {
        _ = _context.Categories.Remove(category);
        _ = await _context.SaveChangesAsync();
    }
}