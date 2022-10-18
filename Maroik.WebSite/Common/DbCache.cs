using Microsoft.EntityFrameworkCore;
using Maroik.WebSite.Models;
using System.Collections.Generic;
using Maroik.Common.DataAccess.Models;

namespace Maroik.WebSite.Common
{
    public static class DbCache
    {
        public static IEnumerable<Category> AdminCategories = null;
        public static IEnumerable<SubCategory> AdminSubCategories = null;

        public static IEnumerable<Category> UserCategories = null;
        public static IEnumerable<SubCategory> UserSubCategories = null;

        public static IEnumerable<Category> AnonymousCategories = null;
        public static IEnumerable<SubCategory> AnonymousSubCategories = null;
    }
}