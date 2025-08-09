using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository
    {
        /// <summary>
        /// 모든 카테고리들을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetCategoryAsync()
        {
            return await context.Categories.ToListAsync();
        }
        /// <summary>
        /// 권한에 따른 카테고리들을 구합니다.
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role)
        {
            return await context.Categories
                    .Where(a => a.Role == role)
                    .OrderBy(a => a.Order)
                    .ToListAsync();
        }
        /// <summary>
        /// 카테고리를 생성합니다.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task CreateCategoryAsync(Category category)
        {
            _ = await context.Categories.AddAsync(category);
            _ = await context.SaveChangesAsync();
        }
        /// <summary>
        /// 카테고리를 업데이트합니다.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task UpdateCategoryAsync(Category category)
        {
            _ = context.Categories.Update(category);
            _ = await context.SaveChangesAsync();
        }
        /// <summary>
        /// 카테고리를 제거합니다.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public async Task DeleteCategoryAsync(Category category)
        {
            _ = context.Categories.Remove(category);
            _ = await context.SaveChangesAsync();
        }
    }
}