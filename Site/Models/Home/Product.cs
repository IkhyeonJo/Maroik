using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMVCSample.src.Site.Models.Home
{
    //Attribute ÀßÇÒ °Í...
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Categoria { get; set; }
        public decimal Valor { get; set; }
    }
}