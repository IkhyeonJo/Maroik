using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Contracts
{
    public interface ISubCategoryRepository
    {
        IEnumerable<SubCategory> AdminSubCategories { get; }
        IEnumerable<SubCategory> UserSubCategories { get; }
        void CreateSubCategory(SubCategory subCategory);
        void UpdateSubCategory(SubCategory subCategory);
        void DeleteSubCategory(SubCategory subCategory);
    }
}