using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Common;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Contracts;

namespace MyLaboratory.Site.Services
{
    public class SubCategoryRepository : ISubCategoryRepository
    {
        private readonly ApplicationDbContext context;

        public SubCategoryRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IEnumerable<SubCategory> AdminSubCategories
        {
            get
            {
                return context.SubCategories
                        .Where(a => a.Role == Role.Admin)
                        .OrderBy(a => a.CategoryId)
                        .ThenBy(a => a.Order)
                        .ToList();
            }
        }

        public IEnumerable<SubCategory> UserSubCategories
        {
            get
            {
                return context.SubCategories
                        .Where(a => a.Role == Role.User)
                        .OrderBy(a => a.CategoryId)
                        .ThenBy(a => a.Order)
                        .ToList();
            }
        }

        public void CreateSubCategory(SubCategory subCategory)
        {
            context.SubCategories.Add(subCategory);
            context.SaveChanges();
        }

        public void UpdateSubCategory(SubCategory subCategory)
        {
            context.SubCategories.Update(subCategory);
            context.SaveChanges();
        }
        public void DeleteSubCategory(SubCategory subCategory)
        {
            context.SubCategories.Remove(subCategory);
            context.SaveChanges();
        }
    }
}