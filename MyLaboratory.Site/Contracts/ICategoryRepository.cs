using MyLaboratory.Site.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role);
        Task CreateCategoryAsync(Category category);
        void UpdateCategory(Category category);
        void DeleteCategory(Category category);
    }
}