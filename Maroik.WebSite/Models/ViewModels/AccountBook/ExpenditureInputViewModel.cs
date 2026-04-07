using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.AccountBook;

/// <summary>
/// Form model for creating or editing a single expenditure transaction in the account book.
/// The spending is classified by <see cref="MainClass"/> / <see cref="SubClass"/>
/// (e.g. "ConsumerSpending" / "MealOrEatOutExpenses") and linked to the payment method
/// and the asset account that was debited.
/// </summary>
public class ExpenditureInputViewModel
{
    /// <summary>Database row ID; 0 for a new record, positive for an edit.</summary>
    [Required(ErrorMessage = "Please enter Id")]
    [Display(Name = "Id")]
    public int Id { get; set; }

    /// <summary>Top-level expenditure category (e.g. "ConsumerSpending", "NonConsumerSpending").</summary>
    [Required(ErrorMessage = "Please enter MainClass")]
    [Display(Name = "MainClass")]
    public string MainClass { get; set; }

    /// <summary>Sub-category within the main class (e.g. "MealOrEatOutExpenses", "Tax").</summary>
    [Required(ErrorMessage = "Please enter SubClass")]
    [Display(Name = "SubClass")]
    public string SubClass { get; set; }

    /// <summary>Free-text description of this spending entry.</summary>
    [Required(ErrorMessage = "Please enter Content")]
    [Display(Name = "Content")]
    public string Content { get; set; }

    /// <summary>Amount spent.</summary>
    [Required(ErrorMessage = "Please enter Amount")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    /// <summary>The date the expenditure occurred; set by the controller from the user's time zone.</summary>
    public DateTime Created { get; set; }

    /// <summary>Payment instrument used (e.g. "CreditCard", "Cash").</summary>
    [Required(ErrorMessage = "Please enter PaymentMethod")]
    [Display(Name = "PaymentMethod")]
    public string PaymentMethod { get; set; }

    /// <summary>Optional free-text memo for this expenditure record.</summary>
    [Display(Name = "Note")]
    public string Note { get; set; }

    /// <summary>Name of the asset (product) that was debited for this expenditure.</summary>
    [Required(ErrorMessage = "Please enter MyDepositAsset")]
    [Display(Name = "MyDepositAsset")]
    public string MyDepositAsset { get; set; }
}
