using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Contracts
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> AdminCategories { get; }
        IEnumerable<Category> UserCategories { get; }
        void CreateCategory(Category category);
        void UpdateCategory(Category category);
        void DeleteCategory(Category category);
    }
}