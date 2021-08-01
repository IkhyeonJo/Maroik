using MyLaboratory.Site.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface ISubCategoryRepository
    {
        Task<IEnumerable<SubCategory>> GetSubCategoryByRoleAsync(string role);
        Task CreateSubCategoryAsync(SubCategory subCategory);
        void UpdateSubCategory(SubCategory subCategory);
        void DeleteSubCategory(SubCategory subCategory);
    }
}