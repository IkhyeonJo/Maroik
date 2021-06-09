using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Models;

namespace MyLaboratory.Site.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext context;

        public ProductRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        //public IEnumerable<Product> GetProduct { get => context.Product; }

        //public IEnumerable<Product> GetProducts
        //{
        //    get
        //    {
        //        var list = context.Product.FromSqlRaw("SELECT * FROM Product").ToList();
        //        return list;
        //    }
        //    set
        //    {

        //    }
        //}
    }
}