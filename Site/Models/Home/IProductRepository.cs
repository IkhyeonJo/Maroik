using System.Collections.Generic;
namespace Site.Models.Home
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProduct { get; }
        IEnumerable<Product> GetProducts { get; }
    }
}