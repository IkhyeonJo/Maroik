using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Extensions;
using Maroik.Website.Models.ViewModels.AccountBook;

namespace Maroik.Website.Mappings;

/// <summary>
/// Maps account-book service response objects to display view models.
/// Applies timezone conversion to timestamps and localization to enumeration display values.
/// Keeping this logic here (rather than in the controller) makes it independently testable
/// and prevents the controller from accumulating presentation-layer responsibilities.
/// </summary>
public static class AccountBookViewModelMapper
{
    extension(AssetResponse item)
    {
        /// <summary>Maps a single <see cref="AssetResponse"/> to an <see cref="AssetOutputViewModel"/> for display.</summary>
        public AssetOutputViewModel ToDisplayViewModel(Func<string, string> localize, string timeZoneIanaId) => new()
        {
            ProductName = item.ProductName,
            Item = localize(item.Item ?? ""),
            Amount = item.Amount,
            MonetaryUnit = item.MonetaryUnit,
            Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
            Note = item.Note,
            Deleted = item.Deleted
        };
    }

    extension(IEnumerable<AssetResponse> items)
    {
        /// <summary>Maps all <see cref="AssetResponse"/> items to display view models.</summary>
        public List<AssetOutputViewModel> ToDisplayViewModels(Func<string, string> localize, string timeZoneIanaId)
            => items.Select(i => i.ToDisplayViewModel(localize, timeZoneIanaId)).ToList();
    }

    extension(IncomeResponse item)
    {
        /// <summary>
        /// Maps a single <see cref="IncomeResponse"/> to an <see cref="IncomeOutputViewModel"/> for display.
        /// <paramref name="currencyByProduct"/> maps asset product names to their monetary unit
        /// and is used to populate <see cref="IncomeOutputViewModel.MonetaryUnit"/>.
        /// </summary>
        public IncomeOutputViewModel ToDisplayViewModel(
            IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Note = item.Note
            };
    }

    extension(IEnumerable<IncomeResponse> items)
    {
        /// <summary>Maps all <see cref="IncomeResponse"/> items to display view models.</summary>
        public List<IncomeOutputViewModel> ToDisplayViewModels(
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId)
        {
            var currencyByProduct = assets
                .ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit);
            return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId)).ToList();
        }
    }

    extension(ExpenditureResponse item)
    {
        /// <summary>
        /// Maps a single <see cref="ExpenditureResponse"/> to an <see cref="ExpenditureOutputViewModel"/> for display.
        /// <paramref name="currencyByProduct"/> maps asset product names to their monetary unit.
        /// </summary>
        public ExpenditureOutputViewModel ToDisplayViewModel(
            IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
                PaymentMethod = item.PaymentMethod,
                Note = item.Note,
                MyDepositAsset = item.MyDepositAsset,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId)
            };
    }

    extension(IEnumerable<ExpenditureResponse> items)
    {
        /// <summary>Maps all <see cref="ExpenditureResponse"/> items to display view models.</summary>
        public List<ExpenditureOutputViewModel> ToDisplayViewModels(
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId)
        {
            var currencyByProduct = assets
                .ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit);
            return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId)).ToList();
        }
    }

    extension(AccountBookPageViewModel vm)
    {
        /// <summary>
        /// Fills the current local time fields on <paramref name="vm"/> for pre-populating date/time pickers.
        /// </summary>
        public void PopulateTimeData(string timeZoneIanaId)
        {
            DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId);
            vm.CurrentDate = ct.ToString("yyyy-MM-dd");
            vm.CurrentHour = ct.Hour;
            vm.CurrentMinute = ct.Minute;
            vm.CurrentSecond = ct.Second;
        }
    }
}
