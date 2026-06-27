using Maroik.Core.Contract.Extensions;
using Maroik.WebSite.Models.ViewModels.AccountBook;

namespace Maroik.WebSite.Extensions;

/// <summary>
/// Whole-text search filtering for account-book view models.
/// Extracted from the controller so the per-field Contains logic is in one place
/// and can be unit-tested independently of HTTP infrastructure.
/// </summary>
public static class AccountBookSearchExtensions
{
    /// <summary>
    /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
    /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
    /// </summary>
    public static IEnumerable<AssetOutputViewModel> ApplySearch(
        this IEnumerable<AssetOutputViewModel> items,
        string? search)
    {
        if (string.IsNullOrEmpty(search)) return items;
        return items.Where(i =>
            Has(i.ProductName, search) ||
            Has(i.Item, search) ||
            Has(i.Amount.TrimTrailingZeros(), search) ||
            Has(i.MonetaryUnit, search) ||
            Has(i.Created.ToString(), search) ||
            Has(i.Updated.ToString(), search) ||
            Has(i.Note, search) ||
            Has(i.Deleted.ToString(), search));
    }

    /// <summary>
    /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
    /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
    /// </summary>
    public static IEnumerable<IncomeOutputViewModel> ApplySearch(
        this IEnumerable<IncomeOutputViewModel> items,
        string? search)
    {
        if (string.IsNullOrEmpty(search)) return items;
        return items.Where(i =>
            Has(i.MainClass, search) ||
            Has(i.SubClass, search) ||
            Has(i.Content, search) ||
            Has(i.Amount.TrimTrailingZeros(), search) ||
            Has(i.MonetaryUnit, search) ||
            Has(i.DepositMyAssetProductName, search) ||
            Has(i.Created.ToString(), search) ||
            Has(i.Updated.ToString(), search) ||
            Has(i.Note, search));
    }

    /// <summary>
    /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
    /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
    /// </summary>
    public static IEnumerable<ExpenditureOutputViewModel> ApplySearch(
        this IEnumerable<ExpenditureOutputViewModel> items,
        string? search)
    {
        if (string.IsNullOrEmpty(search)) return items;
        return items.Where(i =>
            Has(i.MainClass, search) ||
            Has(i.SubClass, search) ||
            Has(i.Content, search) ||
            Has(i.Amount.TrimTrailingZeros(), search) ||
            Has(i.MonetaryUnit, search) ||
            Has(i.PaymentMethod, search) ||
            Has(i.Note, search) ||
            Has(i.MyDepositAsset, search) ||
            Has(i.Created.ToString(), search) ||
            Has(i.Updated.ToString(), search));
    }

    private static bool Has(string? value, string search)
        => (value ?? "").Contains(search);
}
