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

    /// <summary>A UTC value in the time zone; unchanged when the zone is invalid.</summary>
    DateTime ToTimeZone(DateTime utc, string timeZoneIanaId);

    /// <summary>A local value in UTC.</summary>
    DateTime ToUtcFromTimeZone(DateTime local, string timeZoneIanaId);

    /// <summary>A UTC value as a viewer sees it: local time, or ISO-8601 UTC for one who does not see local time.</summary>
    string FormatForViewer(DateTime utc, bool viewerSeesLocalTime, string timeZoneIanaId);

    /// <summary>A time zone's standard name, or the ID itself when it is unknown.</summary>
    string GetTimeZoneDisplayName(string timeZoneIanaId);

    /// <summary>Whether the viewer wrote the content.</summary>
    bool IsContentOwner(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether the viewer wrote the content or is an administrator.</summary>
    bool IsContentOwnerOrAdmin(AccountResponse viewer, string? contentWriter);

    /// <summary>Whether the viewer sees local times rather than raw UTC.</summary>
    bool SeesLocalTime(AccountResponse viewer);

    /// <summary>The nicknames of the administrators among the accounts.</summary>
    HashSet<string> GetAdminNicknames(IEnumerable<AccountResponse> accounts);

    /// <summary>Whether a post's title may be shown to the viewer, an administrator seeing it too.</summary>
    bool IsTitleVisibleToOwnerOrAdmin(BoardResponse board, AccountResponse viewer);

    /// <summary>Whether a post's title may be shown to the viewer, with no administrator bypass.</summary>
    bool IsTitleVisibleToOwner(BoardResponse board, AccountResponse viewer);

    /// <summary>What the finance forms offer and mirror.</summary>
    FinanceFormRules GetFinanceFormRules();

    /// <summary>The reminder limits the calendar pages mirror.</summary>
    CalendarFormRules GetCalendarFormRules();

    /// <summary>The upload timings the File Manager page follows.</summary>
    FileManagerFormRules GetFileManagerFormRules();

    /// <summary>What the budget page needs to know about one sheet kind.</summary>
    BudgetPageRules GetBudgetPageRules(BudgetSheetKind kind);
}
