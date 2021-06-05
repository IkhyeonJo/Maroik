using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Site.Models.Home
{
    public class ProdutoRepository : IRepository
    {
        private readonly AppDbContext context;

        public ProdutoRepository(AppDbContext context)
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