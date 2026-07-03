using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Policies;
using Maroik.Website.Models.ViewModels.Notice;

namespace Maroik.Website.Mappings;

/// <summary>
/// Maps fixed-income and fixed-expenditure response objects to display view models.
/// Schedule status flags (<c>Noticed</c>, <c>Expired</c>) are computed via
/// <see cref="FixedSchedulePolicy"/>, which is shared with the service layer.
/// </summary>
public static class NoticeViewModelMapper
{
    /// <summary>Maps a single <see cref="FixedIncomeResponse"/> to a <see cref="FixedIncomeOutputViewModel"/> for display.</summary>
    public static FixedIncomeOutputViewModel ToDisplayViewModel(
        this FixedIncomeResponse item,
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

    /// <summary>Maps all <see cref="FixedIncomeResponse"/> items to display view models.</summary>
    public static List<FixedIncomeOutputViewModel> ToDisplayViewModels(
        this IEnumerable<FixedIncomeResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId,
        int noticeWindowDays)
    {
        var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => (string?)a.MonetaryUnit);
        var today = DateTime.Today;
        return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays)).ToList();
    }

    /// <summary>Maps a single <see cref="FixedExpenditureResponse"/> to a <see cref="FixedExpenditureOutputViewModel"/> for display.</summary>
    public static FixedExpenditureOutputViewModel ToDisplayViewModel(
        this FixedExpenditureResponse item,
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

    /// <summary>Maps all <see cref="FixedExpenditureResponse"/> items to display view models.</summary>
    public static List<FixedExpenditureOutputViewModel> ToDisplayViewModels(
        this IEnumerable<FixedExpenditureResponse> items,
        IEnumerable<AssetResponse> assets,
        Func<string, string> localize,
        string timeZoneIanaId,
        int noticeWindowDays)
    {
        var currencyByProduct = assets.ToDictionary(a => a.ProductName ?? "", a => (string?)a.MonetaryUnit);
        var today = DateTime.Today;
        return items.Select(i => i.ToDisplayViewModel(currencyByProduct, localize, timeZoneIanaId, today, noticeWindowDays)).ToList();
    }
}
