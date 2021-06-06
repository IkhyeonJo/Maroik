using Site.Database.Tables;
using System.Collections.Generic;

namespace Site.Database.Repositories.Interface
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProduct { get; }
        IEnumerable<Product> GetProducts { get; }
    }
}