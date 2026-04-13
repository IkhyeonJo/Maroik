using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.AccountBook;

/// <summary>
/// Form model for creating or editing a single income transaction in the account book.
/// The income is classified by <see cref="MainClass"/> / <see cref="SubClass"/>
/// (e.g. "RegularIncome" / "LaborIncome") and linked to the asset that received the deposit.
/// </summary>
public class IncomeInputViewModel
{
    /// <summary>Database row ID; 0 for a new record, positive for an edit.</summary>
    [Required(ErrorMessage = "Please enter Id")]
    [Display(Name = "Id")]
    public int Id { get; set; }

    /// <summary>Top-level income category (e.g. "RegularIncome", "IrregularIncome").</summary>
    [Required(ErrorMessage = "Please enter MainClass")]
    [Display(Name = "MainClass")]
    public string? MainClass { get; set; }

    /// <summary>Sub-category within the main class (e.g. "LaborIncome", "FinancialIncome").</summary>
    [Required(ErrorMessage = "Please enter SubClass")]
    [Display(Name = "SubClass")]
    public string? SubClass { get; set; }

    /// <summary>Free-text description of this income entry.</summary>
    [Required(ErrorMessage = "Please enter Content")]
    [Display(Name = "Content")]
    public string? Content { get; set; }

    /// <summary>Amount of income received.</summary>
    [Required(ErrorMessage = "Please enter Amount")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    /// <summary>The date the income was received; set by the controller from the user's time zone.</summary>
    public DateTime Created { get; set; }

    /// <summary>Name of the asset (product) into which this income was deposited.</summary>
    [Required(ErrorMessage = "Please enter DepositMyAssetProductName")]
    [Display(Name = "DepositMyAssetProductName")]
    public string? DepositMyAssetProductName { get; set; }

    /// <summary>Optional free-text memo for this income record.</summary>
    [Display(Name = "Note")]
    public string? Note { get; set; }
}
