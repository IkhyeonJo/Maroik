using Maroik.Core.Service.Constants;
using System.ComponentModel.DataAnnotations;

namespace Maroik.Website.Models.ViewModels.AccountBooks;

/// <summary>Which sheet a request is about.</summary>
public class BudgetSheetInputViewModel
{
    /// <summary>Income or Expenditure.</summary>
    [Required]
    [RegularExpression(PolicyLimits.BudgetSheetKindPattern)]
    public string Kind { get; set; } = "";

    /// <summary>The year.</summary>
    public int Year { get; set; }
}
