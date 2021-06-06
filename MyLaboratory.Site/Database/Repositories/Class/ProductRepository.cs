using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Site.Database.Config;
using Site.Database.Repositories.Interface;
using Site.Database.Tables;

namespace Site.Database.Repositories.Class
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext context;

        public ProductRepository(AppDbContext context)
        {
            this.context = context;
        }

        public IEnumerable<Product> GetProduct { get => context.Product; }

        public IEnumerable<Product> GetProducts
        {
            get
            {
                var list = context.Product.FromSqlRaw("SELECT * FROM Product").ToList();
                return list;
            }
            set
            {

            }
        }
    }
}