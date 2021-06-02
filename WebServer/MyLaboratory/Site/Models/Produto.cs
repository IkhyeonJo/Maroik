namespace AspNetCoreMVCSample.src.Site.Models
{
    public class Produto
    {
        public Produto(string nome = null, string categoria = null, decimal valor = 0)
        {            
            this.Nome = nome;
            this.Categoria = categoria;
            this.Valor = valor;

        }
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Categoria { get; set; }
        public decimal Valor { get; set; }

    }
}