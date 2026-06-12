using Maroik.Core.Contract.Dtos;

namespace Maroik.WebSite.Models.ViewModels.AccountBook;

/// <summary>
/// Page-level view model for the Asset, Income, and Expenditure pages.
/// Carries the active asset list for dropdowns and the user's current local time
/// for pre-populating date/time pickers.
/// </summary>
public class AccountBookPageViewModel
{
    public List<AssetResponse> Assets { get; set; } = [];
    public string CurrentDate { get; set; } = "";
    public int CurrentHour { get; set; }
    public int CurrentMinute { get; set; }
    public int CurrentSecond { get; set; }
}
