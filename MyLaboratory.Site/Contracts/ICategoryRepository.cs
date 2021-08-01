using MyLaboratory.Site.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> UserCategories { get; }
        Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role);
        void CreateCategory(Category category);
        void UpdateCategory(Category category);
        void DeleteCategory(Category category);
    }
}