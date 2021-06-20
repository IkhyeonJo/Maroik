using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Services
{
    public interface ISubCategoryRepository
    {
        IEnumerable<SubCategory> AdminSubCategories { get; }
        IEnumerable<SubCategory> UserSubCategories { get; }
    }
}