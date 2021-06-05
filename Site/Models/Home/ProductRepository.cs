using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Site.Models.Home
{
    public class ProductRepository : IRepository
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