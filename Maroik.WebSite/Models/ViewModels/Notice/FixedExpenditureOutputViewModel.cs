using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.Notice
{
    public class FixedExpenditureOutputViewModel
    {
        [Required(ErrorMessage = "Please enter Id")]
        [Display(Name = "Id")]
        public int Id { get; set; }
        [Required(ErrorMessage = "Please enter MainClass")]
        [Display(Name = "MainClass")]
        public string MainClass { get; set; }
        [Required(ErrorMessage = "Please enter SubClass")]
        [Display(Name = "SubClass")]
        public string SubClass { get; set; }
        [Required(ErrorMessage = "Please enter Contents")]
        [Display(Name = "Contents")]
        public string Contents { get; set; }
        [Required(ErrorMessage = "Please enter Amount")]
        [Display(Name = "Amount")]
        public long Amount { get; set; }
        [Required(ErrorMessage = "Please enter PaymentMethod")]
        [Display(Name = "PaymentMethod")]
        public string PaymentMethod { get; set; }
        [Required(ErrorMessage = "Please enter MyDepositAsset")]
        [Display(Name = "MyDepositAsset")]
        public string MyDepositAsset { get; set; }
        [Required(ErrorMessage = "Please enter DepositMonth")]
        [Display(Name = "DepositMonth")]
        public byte DepositMonth { get; set; }
        [Required(ErrorMessage = "Please enter DepositDay")]
        [Display(Name = "DepositDay")]
        public byte DepositDay { get; set; }
        [Required(ErrorMessage = "Please enter MaturityDate")]
        [Display(Name = "MaturityDate")]
        public string MaturityDate { get; set; }
        [Required(ErrorMessage = "Please enter Created")]
        [Display(Name = "Created")]
        public DateTime Created { get; set; }
        [Required(ErrorMessage = "Please enter Updated")]
        [Display(Name = "Updated")]
        public DateTime Updated { get; set; }
        [Display(Name = "Note")]
        public string Note { get; set; }
        public bool Noticed { get; set; }
        public bool Expired { get; set; }
        [Display(Name = "Unpunctuality")]
        public bool Unpunctuality { get; set; }
    }
}