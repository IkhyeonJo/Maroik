using Microsoft.EntityFrameworkCore;
using MyLaboratory.Blog.Models;
using System.Collections.Generic;
using MyLaboratory.Common.DataAccess.Models;

namespace MyLaboratory.Blog.Common
{
    public static class DbCache
    {
        public static IEnumerable<Category> AdminCategories = null;
        public static IEnumerable<SubCategory> AdminSubCategories = null;

        public static IEnumerable<Category> UserCategories = null;
        public static IEnumerable<SubCategory> UserSubCategories = null;
    }
}