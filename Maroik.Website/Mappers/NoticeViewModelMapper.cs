using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.Notices;

namespace Maroik.Website.Mappers;

/// <summary>
/// Maps fixed-income and fixed-expenditure response objects to display view models.
/// Schedule status flags (<c>Noticed</c>, <c>Expired</c>) are computed via
/// <c>FixedSchedulePolicy</c>, which is shared with the service layer.
/// </summary>
public static class NoticeViewModelMapper
{
    extension(FixedScheduleRowState status)
    {
        /// <summary>
        /// Maps a <see cref="FixedScheduleRowState"/> to the notice-grid's Bootstrap row class.
        /// The status decision is Domain's (<c>FixedSchedulePolicy.GetRowStatus</c>); this
        /// presentation-only mapping (and the "clsGridRow" selector hook the client script binds
        /// to) belongs here, not in Domain.
        /// </summary>
        public string ToRowCssClass() => status switch
        {
            FixedScheduleRowState.Expired => "table-danger clsGridRow",
            FixedScheduleRowState.Noticed => "table-info clsGridRow",
            _ => "table-inactive clsGridRow"
        };
    }

    extension(FixedIncomeResponse item)
    {
        /// <summary>Maps a single <see cref="FixedIncomeResponse"/> to a <see cref="FixedIncomeOutputViewModel"/> for display.</summary>
        private FixedIncomeOutputViewModel ToDisplayViewModel(
            IFinancePolicyService financePolicy,
            ITimeZoneDisplayService timeZones,
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
                MaturityDate = item.MaturityDate.ToMaturityDateString(),
                Note = item.Note,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = timeZones.ToTimeZone(item.Created, timeZoneIanaId),
                Updated = timeZones.ToTimeZone(item.Updated, timeZoneIanaId),
                Noticed = financePolicy.IsFixedScheduleNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
                Expired = financePolicy.IsFixedScheduleExpired(item.MaturityDate, today),
                RowCssClass = financePolicy.GetFixedScheduleRowState(
                    financePolicy.IsFixedScheduleExpired(item.MaturityDate, today),
                    financePolicy.IsFixedScheduleNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality)).ToRowCssClass(),
                Unpunctuality = item.Unpunctuality
            };
    }

    extension(IEnumerable<FixedIncomeResponse> items)
    {
        /// <summary>
        /// Maps all <see cref="FixedIncomeResponse"/> items to display view models; their notice / expiry flags are judged
        /// against <paramref name="utcNow"/> (the request's clock reading) as a date in <paramref name="timeZoneIanaId"/>.
        /// </summary>
        public List<FixedIncomeOutputViewModel> ToDisplayViewModels(
            IFinancePolicyService financePolicy,
            ITimeZoneDisplayService timeZones,
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId,
            int noticeWindowDays,
            DateTime utcNow)
        {
            var currencyByProduct = assets
                .GroupBy(a => a.ProductName ?? "")
                .ToDictionary(g => g.Key, g => g.Last().MonetaryUnit);
            var today = timeZones.ToTimeZone(utcNow, timeZoneIanaId).Date;
            return
            [
                .. items.Select(i => i.ToDisplayViewModel(financePolicy, timeZones, currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays))
            ];
        }
    }

    extension(FixedExpenditureResponse item)
    {
        /// <summary>Maps a single <see cref="FixedExpenditureResponse"/> to a <see cref="FixedExpenditureOutputViewModel"/> for display.</summary>
        private FixedExpenditureOutputViewModel ToDisplayViewModel(
            IFinancePolicyService financePolicy,
            ITimeZoneDisplayService timeZones,
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
                MaturityDate = item.MaturityDate.ToMaturityDateString(),
                Note = item.Note,
                Created = timeZones.ToTimeZone(item.Created, timeZoneIanaId),
                Updated = timeZones.ToTimeZone(item.Updated, timeZoneIanaId),
                Noticed = financePolicy.IsFixedScheduleNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
                Expired = financePolicy.IsFixedScheduleExpired(item.MaturityDate, today),
                RowCssClass = financePolicy.GetFixedScheduleRowState(
                    financePolicy.IsFixedScheduleExpired(item.MaturityDate, today),
                    financePolicy.IsFixedScheduleNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality)).ToRowCssClass(),
                Unpunctuality = item.Unpunctuality
            };
    }

    extension(IEnumerable<FixedExpenditureResponse> items)
    {
        /// <summary>
        /// Maps all <see cref="FixedExpenditureResponse"/> items to display view models; their notice / expiry flags are judged
        /// against <paramref name="utcNow"/> (the request's clock reading) as a date in <paramref name="timeZoneIanaId"/>.
        /// </summary>
        public List<FixedExpenditureOutputViewModel> ToDisplayViewModels(
            IFinancePolicyService financePolicy,
            ITimeZoneDisplayService timeZones,
            IEnumerable<AssetResponse> assets,
            Func<string, string> localize,
            string timeZoneIanaId,
            int noticeWindowDays,
            DateTime utcNow)
        {
            var currencyByProduct = assets
                .GroupBy(a => a.ProductName ?? "")
                .ToDictionary(g => g.Key, g => g.Last().MonetaryUnit);
            var today = timeZones.ToTimeZone(utcNow, timeZoneIanaId).Date;
            return
            [
                .. items.Select(i => i.ToDisplayViewModel(financePolicy, timeZones, currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays))
            ];
        }
    }
}
