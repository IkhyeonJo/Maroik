using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;

namespace Maroik.WebSite.Utilities;

/// <summary>
/// Maps fixed-income and fixed-expenditure response objects to display view models.
/// Computes the <c>Noticed</c> and <c>Expired</c> presentation flags so the controller
/// does not need to contain date-comparison logic.
/// </summary>
public static class NoticeViewModelMapper
{
    /// <summary>
    /// Returns <see langword="true"/> when the next scheduled deposit date is within
    /// <paramref name="noticeWindowDays"/> days of <paramref name="today"/>,
    /// or when <paramref name="unpunctuality"/> is set.
    /// Returns <see langword="false"/> on invalid dates (e.g. Feb 29 in a non-leap year).
    /// </summary>
    public static bool IsNoticed(int depositMonth, int depositDay, DateTime today, int noticeWindowDays, bool unpunctuality)
    {
        if (unpunctuality) return true;
        try
        {
            var depositDate = new DateTime(today.Year, depositMonth, depositDay);
            double daysUntil = (depositDate - today).TotalDays;
            return daysUntil >= 0 && daysUntil <= noticeWindowDays;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Returns <see langword="true"/> when <paramref name="maturityDate"/> is in the past relative to <paramref name="today"/>.</summary>
    public static bool IsExpired(DateTime maturityDate, DateTime today)
        => (maturityDate - today).TotalDays < 0;

    /// <summary>Maps a single <see cref="FixedIncomeResponse"/> to a <see cref="FixedIncomeOutputViewModel"/> for display.</summary>
    public static FixedIncomeOutputViewModel ToDisplayViewModel(
        this FixedIncomeResponse item,
        IReadOnlyDictionary<string, string?> currencyByProduct,
        Func<string, string> localize,
        string timeZoneIanaId,
        DateTime today,
        int noticeWindowDays) => new()
    {
        Id                        = item.Id,
        MainClass                 = localize(item.MainClass ?? ""),
        SubClass                  = localize(item.SubClass ?? ""),
        Content                   = item.Content,
        Amount                    = item.Amount,
        MonetaryUnit              = currencyByProduct.GetValueOrDefault(item.DepositMyAssetProductName ?? ""),
        DepositMonth              = item.DepositMonth,
        DepositDay                = item.DepositDay,
        MaturityDate              = item.MaturityDate.ToString("yyyy-MM-dd"),
        Note                      = item.Note,
        DepositMyAssetProductName = item.DepositMyAssetProductName,
        Created                   = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
        Updated                   = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
        Noticed                   = IsNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
        Expired                   = IsExpired(item.MaturityDate, today),
        Unpunctuality             = item.Unpunctuality
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
        Id             = item.Id,
        MainClass      = localize(item.MainClass ?? ""),
        SubClass       = localize(item.SubClass ?? ""),
        Content        = item.Content,
        Amount         = item.Amount,
        MonetaryUnit   = currencyByProduct.GetValueOrDefault(item.PaymentMethod ?? ""),
        PaymentMethod  = item.PaymentMethod,
        MyDepositAsset = item.MyDepositAsset,
        DepositMonth   = item.DepositMonth,
        DepositDay     = item.DepositDay,
        MaturityDate   = item.MaturityDate.ToString("yyyy-MM-dd"),
        Note           = item.Note,
        Created        = item.Created.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
        Updated        = item.Updated.ConvertTimeByTimeZoneIanaId(timeZoneIanaId),
        Noticed        = IsNoticed(item.DepositMonth, item.DepositDay, today, noticeWindowDays, item.Unpunctuality),
        Expired        = IsExpired(item.MaturityDate, today),
        Unpunctuality  = item.Unpunctuality
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
