namespace Maroik.Core.Domain.Accounts;

/// <summary>What an administrator's save asks of an account (see <see cref="Account.ApplyAdminChanges"/>); a null value leaves that field as it is.</summary>
public sealed record AdminAccountChanges(
    string? TimeZoneIanaId,
    long? FileManagerQuotaBytes,
    bool? FileManagerScanDisabled,
    string? Role,
    bool Locked,
    bool EmailConfirmed,
    bool AgreedServiceTerms,
    bool Deleted,
    string? Message);
