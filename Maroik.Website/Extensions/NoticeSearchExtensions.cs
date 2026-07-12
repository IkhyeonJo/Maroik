using Maroik.Core.Contract.Extensions;
using Maroik.Website.Models.ViewModels.Notice;

namespace Maroik.Website.Extensions;

/// <summary>
/// Whole-text search filtering for Notice (fixed income / fixed expenditure) view models.
/// </summary>
public static class NoticeSearchExtensions
{
    extension(IEnumerable<FixedIncomeOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those containing <paramref name="search"/> in any display field.
        /// Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<FixedIncomeOutputViewModel> ApplySearch(string? search)
        {
            if (string.IsNullOrEmpty(search)) return items;
            return items.Where(i =>
                Has(i.MainClass, search) ||
                Has(i.SubClass, search) ||
                Has(i.Content, search) ||
                Has(i.Amount.TrimTrailingZeros(), search) ||
                Has(i.MonetaryUnit, search) ||
                Has(i.DepositMonth.ToString(), search) ||
                Has(i.DepositDay.ToString(), search) ||
                Has(i.MaturityDate, search) ||
                Has(i.Note, search) ||
                Has(i.DepositMyAssetProductName, search) ||
                Has(i.Created.ToString(), search) ||
                Has(i.Updated.ToString(), search));
        }
    }

    extension(IEnumerable<FixedExpenditureOutputViewModel> items)
    {
        /// <summary>
        /// Filters <paramref name="items"/> to those containing <paramref name="search"/> in any display field.
        /// Returns all items when <paramref name="search"/> is null or empty.
        /// </summary>
        public IEnumerable<FixedExpenditureOutputViewModel> ApplySearch(string? search)
        {
            if (string.IsNullOrEmpty(search)) return items;
            return items.Where(i =>
                Has(i.MainClass, search) ||
                Has(i.SubClass, search) ||
                Has(i.Content, search) ||
                Has(i.Amount.TrimTrailingZeros(), search) ||
                Has(i.MonetaryUnit, search) ||
                Has(i.PaymentMethod, search) ||
                Has(i.MyDepositAsset, search) ||
                Has(i.DepositMonth.ToString(), search) ||
                Has(i.DepositDay.ToString(), search) ||
                Has(i.MaturityDate, search) ||
                Has(i.Created.ToString(), search) ||
                Has(i.Updated.ToString(), search) ||
                Has(i.Note, search));
        }
    }

    private static bool Has(string? value, string search) => (value ?? "").Contains(search);
}
