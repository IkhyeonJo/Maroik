using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// The Domain's stateless rules and the values the pages mirror, for the Website, which reaches the Domain only through the Service
/// layer. Every member answers from a Domain policy and decides nothing itself.
/// </summary>
public interface IPolicyService
{
    /// <summary>Reads the text an amount field posted; false when it is not an amount.</summary>
    bool TryParseAmount(string? input, out decimal amount);

    /// <summary>Whether an uploaded image may have this extension.</summary>
    bool IsAllowedImageExtension(string? extension);

    /// <summary>The content types an uploaded image may have.</summary>
    IReadOnlyList<string> GetAllowedImageContentTypes();

    /// <summary>Turns a File Manager quota typed in GB into bytes, or refuses a negative or oversized value.</summary>
    (ServiceResult Result, long Bytes) FileManagerQuotaBytesFromGb(decimal gb);

    /// <summary>A File Manager quota in GB, for the admin form.</summary>
    decimal FileManagerQuotaGbFromBytes(long bytes);

    /// <summary>Whether a password fits the longest one the hash can take.</summary>
    bool IsPasswordWithinMaxByteLength(string? password);

    /// <summary>Whether a date lies in the years a calendar event may use.</summary>
    bool IsWithinCalendarEventYears(DateTime date);

    /// <summary>Whether an expenditure subclass is a transfer, which needs a deposit asset.</summary>
    bool IsDepositAssetSubClass(string? subClass);

    /// <summary>Whether a fixed schedule's next deposit is inside the notice window.</summary>
    bool IsFixedScheduleNoticed(int depositMonth, int depositDay, DateTime today, int noticeWindowDays, bool unpunctuality);

    /// <summary>Whether a fixed schedule's maturity date has passed.</summary>
    bool IsFixedScheduleExpired(DateTime maturityDate, DateTime today);

    /// <summary>How a fixed schedule's row stands, from its two flags.</summary>
    FixedScheduleRowState GetFixedScheduleRowState(bool expired, bool noticed);

    /// <summary>Why a session no longer holds, or null while it does. <paramref name="current"/> is the account as stored now.</summary>
    string? GetSessionInvalidationReason(AccountResponse? current, AccountResponse session);

    /// <summary>The message a freshly created account carries.</summary>
    string GetAccountCreatedMessage();

    /// <summary>What the finance forms offer and mirror.</summary>
    FinanceFormRules GetFinanceFormRules();

    /// <summary>The reminder limits the calendar pages mirror.</summary>
    CalendarFormRules GetCalendarFormRules();

    /// <summary>The upload timings the File Manager page follows.</summary>
    FileManagerFormRules GetFileManagerFormRules();

    /// <summary>What the budget page needs to know about one sheet kind.</summary>
    BudgetPageRules GetBudgetPageRules(BudgetSheetKind kind);
}
