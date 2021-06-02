using System.Collections.Generic;

namespace AspNetCoreMVCSample.src.Site.Models
{
    public class ProdutoRepository : IRepository
    {
        private readonly AppDbContext _ctx;

        public ProdutoRepository(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public IEnumerable<Produto> GetProdutos { get => _ctx.Produtos; }
    }
}