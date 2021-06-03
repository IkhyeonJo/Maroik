using System.Collections.Generic;

namespace AspNetCoreMVCSample.src.Site.Models
{
    public class ProductRepository : IRepository
    {
        private readonly AppDbContext _ctx;

        public ProductRepository(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public IEnumerable<Product> GetProduct { get => _ctx.Product; }
    }
}