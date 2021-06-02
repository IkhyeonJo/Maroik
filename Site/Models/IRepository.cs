using System.Collections.Generic;
namespace AspNetCoreMVCSample.src.Site.Models
{
    public interface IRepository
    {
        IEnumerable<Produto> GetProdutos { get; }
    }
}