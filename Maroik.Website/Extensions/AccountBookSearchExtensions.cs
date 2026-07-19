using System.Globalization;
using Maroik.Core.Contract.Extensions;
using Maroik.Website.Models.ViewModels.AccountBook;

namespace Maroik.Website.Extensions;

/// <summary>
/// Whole-text search filtering for account-book view models.
/// Extracted from the controller so the per-field Contains logic is in one place
/// and can be unit-tested independently of HTTP infrastructure.
/// </summary>
public static class AccountBookSearchExtensions
{
    extension(IEnumerable<AssetOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
        /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<AssetOutputViewModel> ApplySearch(string? search)
        {
            if (string.IsNullOrEmpty(search)) return items;
            return items.Where(i =>
                Has(i.ProductName, search) ||
                Has(i.Item, search) ||
                Has(i.Amount.TrimTrailingZeros(), search) ||
                Has(i.MonetaryUnit, search) ||
                Has(i.Created.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Updated.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Note, search) ||
                Has(i.Deleted.ToString(), search));
        }
    }

    extension(IEnumerable<IncomeOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
        /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<IncomeOutputViewModel> ApplySearch(string? search)
        {
            if (string.IsNullOrEmpty(search)) return items;
            return items.Where(i =>
                Has(i.MainClass, search) ||
                Has(i.SubClass, search) ||
                Has(i.Content, search) ||
                Has(i.Amount.TrimTrailingZeros(), search) ||
                Has(i.MonetaryUnit, search) ||
                Has(i.DepositMyAssetProductName, search) ||
                Has(i.Created.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Updated.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Note, search));
        }
    }

    extension(IEnumerable<ExpenditureOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those that contain <paramref name="search"/>
        /// in any display field. Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<ExpenditureOutputViewModel> ApplySearch(string? search)
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
                Has(i.Created.ToString(CultureInfo.InvariantCulture), search) ||
                Has(i.Updated.ToString(CultureInfo.InvariantCulture), search));
        }
    }

    private static bool Has(string? value, string search)
        => (value ?? "").Contains(search);
}
