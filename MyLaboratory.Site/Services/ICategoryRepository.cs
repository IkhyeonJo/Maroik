using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Services
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> AdminCategories { get; }
        IEnumerable<Category> UserCategories { get; }
    }
}