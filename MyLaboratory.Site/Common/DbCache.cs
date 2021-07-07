using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Common
{
    public static class DbCache
    {
        public static IEnumerable<Category> AdminCategories = null;
        public static IEnumerable<SubCategory> AdminSubCategories = null;

        public static IEnumerable<Category> UserCategories = null;
        public static IEnumerable<SubCategory> UserSubCategories = null;
    }
}