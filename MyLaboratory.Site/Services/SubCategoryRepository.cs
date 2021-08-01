using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Common;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Contracts;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Services
{
    public class SubCategoryRepository : ISubCategoryRepository
    {
        private readonly ApplicationDbContext context;

        public SubCategoryRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        public async Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role)
        {
            return await context.SubCategories
                    .Where(a => a.Role == role)
                    .OrderBy(a => a.Order)
                    .ToListAsync();
        }

        public async Task CreateSubCategoryAsync(SubCategory subCategory)
        {
            await context.SubCategories.AddAsync(subCategory);
            await context.SaveChangesAsync();
        }

        public async Task UpdateSubCategoryAsync(SubCategory subCategory)
        {
            context.SubCategories.Update(subCategory);
            await context.SaveChangesAsync();
        }
        public void DeleteSubCategory(SubCategory subCategory)
        {
            context.SubCategories.Remove(subCategory);
            context.SaveChanges();
        }
    }
}