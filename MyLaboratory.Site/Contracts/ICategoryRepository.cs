using MyLaboratory.Site.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategoryByRoleAsync(string role);
        Task CreateCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);
        Task DeleteCategoryAsync(Category category);
    }
}