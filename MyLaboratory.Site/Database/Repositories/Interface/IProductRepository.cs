using MyLaboratory.Site.Database.Tables;
using System.Collections.Generic;

namespace MyLaboratory.Site.Database.Repositories.Interface
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProduct { get; }
        IEnumerable<Product> GetProducts { get; }
    }
}