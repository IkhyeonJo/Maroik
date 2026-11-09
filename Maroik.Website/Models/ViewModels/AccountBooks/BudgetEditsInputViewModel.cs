using Maroik.Core.Service.Constants;
using System.ComponentModel.DataAnnotations;

namespace Maroik.Website.Models.ViewModels.AccountBooks;

/// <summary>Typed edits of a sheet's cells (one, or several for an undo / redo of a paste).</summary>
public sealed class BudgetEditsInputViewModel : BudgetSheetInputViewModel
{
    /// <summary>The edits.</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(PolicyLimits.BudgetMaxEditsPerRequest)]
    public List<BudgetCellEditInputViewModel> Edits { get; set; } = [];
}
