using MyLaboratory.Site.Models;
using System.Collections.Generic;

namespace MyLaboratory.Site.Contracts
{
    public interface IProductRepository
    {
        IEnumerable<Product> GetProduct { get; }
        IEnumerable<Product> GetProducts { get; }
    }
}