using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models;

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
                return context.Categories.FromSqlRaw($"SELECT * FROM Category WHERE `Role` = '{Role.Admin}' ORDER BY `Order`;").ToList();

            }
        }

        public IEnumerable<Category> UserCategories
        {
            get
            {
                return context.Categories.FromSqlRaw($"SELECT * FROM Category WHERE `Role` = '{Role.User}' ORDER BY `Order`;").ToList();
            }
        }
    }
}