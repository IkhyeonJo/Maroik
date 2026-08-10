using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Core.Domain.Finance;
using Maroik.Website.Models.ViewModels.Notice;

namespace Maroik.Website.Mappings;

/// <summary>
/// Maps fixed-income and fixed-expenditure response objects to display view models.
/// Schedule status flags (<c>Noticed</c>, <c>Expired</c>) are computed via
/// <see cref="FixedSchedulePolicy"/>, which is shared with the service layer.
/// </summary>
public static class NoticeViewModelMapper
{
    extension(FixedIncomeResponse item)
    {
        /// <summary>Maps a single <see cref="FixedIncomeResponse"/> to a <see cref="FixedIncomeOutputViewModel"/> for display.</summary>
        private FixedIncomeOutputViewModel ToDisplayViewModel(
            IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId,
            DateTime today,
            int noticeWindowDays) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
                DepositMonth = item.DepositMonth,
                DepositDay = item.DepositDay,
                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                Note = item.Note,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Noticed = FixedSchedulePolicy.IsNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
                Expired = FixedSchedulePolicy.IsExpired(item.MaturityDate, today),
                Unpunctuality = item.Unpunctuality
            };
    }

    extension(IEnumerable<FixedIncomeResponse> items)
    {
        /// <summary>Maps all <see cref="FixedIncomeResponse"/> items to display view models.</summary>
        public List<FixedIncomeOutputViewModel> ToDisplayViewModels(
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId,
            int noticeWindowDays)
        {
            var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit);
            var today = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).Date;
            return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays)).ToList();
        }
    }

    extension(FixedExpenditureResponse item)
    {
        /// <summary>Maps a single <see cref="FixedExpenditureResponse"/> to a <see cref="FixedExpenditureOutputViewModel"/> for display.</summary>
        private FixedExpenditureOutputViewModel ToDisplayViewModel(
            IReadOnlyDictionary<string, string?> currencyByProduct,
            Func<string, string> localize,
            string timeZoneIanaId,
            DateTime today,
            int noticeWindowDays) => new()
            {
                Id = item.Id,
                MainClass = localize(item.MainClass ?? ""),
                SubClass = localize(item.SubClass ?? ""),
                Content = item.Content,
                Amount = item.Amount,
                MonetaryUnit = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
                PaymentMethod = item.PaymentMethod,
                MyDepositAsset = item.MyDepositAsset,
                DepositMonth = item.DepositMonth,
                DepositDay = item.DepositDay,
                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                Note = item.Note,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
                Noticed = FixedSchedulePolicy.IsNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
                Expired = FixedSchedulePolicy.IsExpired(item.MaturityDate, today),
                Unpunctuality = item.Unpunctuality
            };
    }

    extension(IEnumerable<FixedExpenditureResponse> items)
    {
        /// <summary>Maps all <see cref="FixedExpenditureResponse"/> items to display view models.</summary>
        public List<FixedExpenditureOutputViewModel> ToDisplayViewModels(
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId,
            int noticeWindowDays)
        {
            var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => a.MonetaryUnit);
            var today = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).Date;
            return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays)).ToList();
        }
    }
}
