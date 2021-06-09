using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyLaboratory.Site.Models
{
    [Table("Worker")]
    public class Worker
    {
        [Key]
        public int Id { get; set; }
        public string WorkerName { get; set; }
        public string Job { get; set; }
        public decimal Wage { get; set; }
    }
}