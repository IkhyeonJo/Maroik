using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class SubCategoryRepository : ISubCategoryRepository
{
    private readonly ApplicationDbContext _context;

    public SubCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all subcategories.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<SubCategory>> GetSubCategoryAsync()
    {
        return await _context.SubCategories.ToListAsync();
    }
    /// <summary>
    /// Gets subcategories by permission.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public async Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role)
    {
        return await _context.SubCategories
                .Where(a => a.Role == role)
                .OrderBy(a => a.Order)
                .ToListAsync();
    }
    /// <summary>
    /// Creates a subcategory.
    /// </summary>
    /// <param name="subCategory"></param>
    /// <returns></returns>
    public async Task CreateSubCategoryAsync(SubCategory subCategory)
    {
        _ = await _context.SubCategories.AddAsync(subCategory);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Updates a subcategory.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public async Task UpdateSubCategoryAsync(SubCategory subCategory)
    {
        _ = _context.SubCategories.Update(subCategory);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Removes a subcategory.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public async Task DeleteSubCategoryAsync(SubCategory subCategory)
    {
        _ = _context.SubCategories.Remove(subCategory);
        _ = await _context.SaveChangesAsync();
    }
}