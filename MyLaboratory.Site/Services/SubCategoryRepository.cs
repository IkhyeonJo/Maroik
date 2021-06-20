using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models;

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
                return context.SubCategories.FromSqlRaw($"SELECT * FROM SubCategory WHERE `Role` = '{Role.Admin}' ORDER BY CategoryId, `Order`;").ToList();

            }
        }

        public IEnumerable<SubCategory> UserSubCategories
        {
            get
            {
                return context.SubCategories.FromSqlRaw($"SELECT * FROM SubCategory WHERE `Role` = '{Role.User}' ORDER BY CategoryId, `Order`;").ToList();
            }
        }
    }
}