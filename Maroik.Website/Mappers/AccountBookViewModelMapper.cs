using Maroik.Core.Contract.Interfaces;
using System.Globalization;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Constants;
using Maroik.Website.Models.ViewModels.AccountBooks;

namespace Maroik.Website.Mappers;

/// <summary>
/// Maps account-book service response objects to display view models.
/// Applies timezone conversion to timestamps and localization to enumeration display values.
/// Keeping this logic here (rather than in the controller) makes it independently testable
/// and prevents the controller from accumulating presentation-layer responsibilities.
/// </summary>
public static class AccountBookViewModelMapper
{
    /// <summary>The "yyyy-MM-dd HH:mm:ss" shape <see cref="ParseLocalDateTime"/> expects.</summary>
    private const string LocalDateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// True when <paramref name="value"/> parses as <see cref="LocalDateTimeFormat"/> — used by
    /// <see cref="ExpenditureInputViewModel.Validate"/> / <see cref="IncomeInputViewModel.Validate"/>
    /// so a malformed <c>Created</c> value fails cleanly as a validation error instead of throwing
    /// out of the <c>ToCreatedUtc</c> extensions below.
    /// </summary>
    public static bool IsValidLocalDateTime(string? value) =>
        DateTime.TryParseExact(value, LocalDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <summary>
    /// Parses a "yyyy-MM-dd HH:mm:ss" local wall-clock string (no time zone of its own — the
    /// caller applies one via <see cref="ITimeZoneDisplayService.ToUtcFromTimeZone"/>).
    /// </summary>
    private static DateTime ParseLocalDateTime(string value) =>
        DateTime.ParseExact(value, LocalDateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);

    extension(IncomeInputViewModel vm)
    {
        /// <summary>
        /// Converts <see cref="IncomeInputViewModel.Created"/> — the account's own local wall-clock
        /// time, never the browser's — to UTC using <paramref name="accountTimeZoneIanaId"/> (the
        /// logged-in account's own <c>TimeZoneIanaId</c>, already known server-side). The client
        /// never computes or transmits a UTC instant or a time zone itself.
        /// </summary>
        public DateTime ToCreatedUtc(ITimeZoneDisplayService timeZones, string accountTimeZoneIanaId) =>
            timeZones.ToUtcFromTimeZone(ParseLocalDateTime(vm.Created!), accountTimeZoneIanaId);
    }

    extension(ExpenditureInputViewModel vm)
    {
        /// <summary>Same conversion as the <c>IncomeInputViewModel</c> overload above, for an expenditure.</summary>
        public DateTime ToCreatedUtc(ITimeZoneDisplayService timeZones, string accountTimeZoneIanaId) =>
            timeZones.ToUtcFromTimeZone(ParseLocalDateTime(vm.Created!), accountTimeZoneIanaId);
    }

    extension(AssetResponse item)
    {
        /// <summary>Maps a single <see cref="AssetResponse"/> to an <see cref="AssetOutputViewModel"/> for display.</summary>
        public AssetOutputViewModel ToDisplayViewModel(ITimeZoneDisplayService timeZones, Func<string, string> localize, string timeZoneIanaId) => new()
        {
            ProductName = item.ProductName,
            Item = localize(item.Item ?? ""),
            Amount = item.Amount,
            MonetaryUnit = item.MonetaryUnit,
            AmountText = AmountDisplay.Text(item.Amount, item.MonetaryUnit),
            Created = timeZones.ToTimeZone(item.Created, timeZoneIanaId),
            Updated = timeZones.ToTimeZone(item.Updated, timeZoneIanaId),
            Note = item.Note,
            Deleted = item.Deleted
        };
    }

    extension(IEnumerable<AssetResponse> items)
    {
        /// <summary>Maps all <see cref="AssetResponse"/> items to display view models.</summary>
        public List<AssetOutputViewModel> ToDisplayViewModels(ITimeZoneDisplayService timeZones, Func<string, string> localize, string timeZoneIanaId)
            =>
            [
                .. items.Select(i => i.ToDisplayViewModel(timeZones, localize, timeZoneIanaId))
            ];
    }

    extension(IncomeResponse item)
    {
        /// <summary>
        /// Maps a single <see cref="IncomeResponse"/> to an <see cref="IncomeOutputViewModel"/> for display.
        /// <paramref name="currencyByProduct"/> maps asset product names to their monetary unit
        /// and is used to populate <see cref="IncomeOutputViewModel.MonetaryUnit"/>.
        /// </summary>
        public IncomeOutputViewModel ToDisplayViewModel(
            ITimeZoneDisplayService timeZones, IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
                AmountText = AmountDisplay.Text(item.Amount, currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? "")),
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = timeZones.ToTimeZone(item.Created, timeZoneIanaId),
                Updated = timeZones.ToTimeZone(item.Updated, timeZoneIanaId),
                Note = item.Note
            };
    }

    extension(IEnumerable<IncomeResponse> items)
    {
        /// <summary>Maps all <see cref="IncomeResponse"/> items to display view models.</summary>
        public List<IncomeOutputViewModel> ToDisplayViewModels(
            ITimeZoneDisplayService timeZones, IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId)
        {
            var currencyByProduct = assets
                .GroupBy(a => a.ProductName ?? "")
                .ToDictionary(g => g.Key, g => g.Last().MonetaryUnit);
            return
            [
                .. items.Select(i => i.ToDisplayViewModel(timeZones, currencyByProduct, localize, timeZoneIanaId))
            ];
        }
    }

    extension(ExpenditureResponse item)
    {
        /// <summary>
        /// Maps a single <see cref="ExpenditureResponse"/> to an <see cref="ExpenditureOutputViewModel"/> for display.
        /// <paramref name="currencyByProduct"/> maps asset product names to their monetary unit.
        /// </summary>
        public ExpenditureOutputViewModel ToDisplayViewModel(
            ITimeZoneDisplayService timeZones, IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
                AmountText = AmountDisplay.Text(item.Amount, currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? "")),
                PaymentMethod = item.PaymentMethod,
                Note = item.Note,
                MyDepositAsset = item.MyDepositAsset,
                Created = timeZones.ToTimeZone(item.Created, timeZoneIanaId),
                Updated = timeZones.ToTimeZone(item.Updated, timeZoneIanaId)
            };
    }

    extension(IEnumerable<ExpenditureResponse> items)
    {
        /// <summary>
        /// Maps all <see cref="ExpenditureResponse"/> items to display view models; those in <paramref name="unusuallyLarge"/>
        /// carry the localized "Unusually large" mark in <see cref="ExpenditureOutputViewModel.AmountFlag"/>.
        /// </summary>
        public List<ExpenditureOutputViewModel> ToDisplayViewModels(
            ITimeZoneDisplayService timeZones, IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId,
            IReadOnlySet<long> unusuallyLarge)
        {
            var currencyByProduct = assets
                .GroupBy(a => a.ProductName ?? "")
                .ToDictionary(g => g.Key, g => g.Last().MonetaryUnit);
            return
            [
                .. items.Select(i =>
                {
                    ExpenditureOutputViewModel vm = i.ToDisplayViewModel(timeZones, currencyByProduct, localize, timeZoneIanaId);
                    vm.AmountFlag = unusuallyLarge.Contains(i.Id) ? localize("Unusually large") : "";
                    return vm;
                })
            ];
        }
    }

    extension(AccountBookPageViewModel vm)
    {
        /// <summary>
        /// Fills the current local time fields on <paramref name="vm"/> for pre-populating date/time pickers:
        /// <paramref name="utcNow"/> (the request's clock reading) shown in <paramref name="timeZoneIanaId"/>.
        /// </summary>
        public void PopulateTimeData(ITimeZoneDisplayService timeZones, string timeZoneIanaId, DateTime utcNow)
        {
            DateTime ct = timeZones.ToTimeZone(utcNow, timeZoneIanaId);
            vm.CurrentDate = ct.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            vm.CurrentHour = ct.Hour;
            vm.CurrentMinute = ct.Minute;
            vm.CurrentSecond = ct.Second;
        }
    }
}
