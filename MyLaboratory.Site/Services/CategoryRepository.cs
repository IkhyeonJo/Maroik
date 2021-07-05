using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Contracts;

namespace MyLaboratory.Site.Services
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext context;

        public CategoryRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IEnumerable<Category> AdminCategories
        {
            get
            {
                return context.Categories
                        .Where(a => a.Role == Role.Admin)
                        .OrderBy(a => a.Order)
                        .ToList();
            }
        }

        public IEnumerable<Category> UserCategories
        {
            get
            {
                return context.Categories
                        .Where(a => a.Role == Role.User)
                        .OrderBy(a => a.Order)
                        .ToList();
            }
        }

        public void CreateCategory(Category category)
        {
            context.Categories.Add(category);
            context.SaveChanges();
        }
    }
}