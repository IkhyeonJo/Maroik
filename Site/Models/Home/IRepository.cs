using System.Collections.Generic;
namespace Site.Models.Home
{
    public interface IRepository
    {
        IEnumerable<Product> GetProduct { get; }
        IEnumerable<Product> GetProducts { get; }
    }
}