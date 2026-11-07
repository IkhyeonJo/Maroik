namespace Maroik.Core.Domain.Accounts;

/// <summary>Why a signed-in session no longer stands for its account (<see cref="AccountSessionPolicy"/>).</summary>
public enum SessionInvalidationReason
{
    /// <summary>The session's account has no row any more.</summary>
    AccountMissing,

    /// <summary>The session's account is soft-deleted.</summary>
    AccountDeleted,

    /// <summary>The account's security stamp moved on (a password change or reset, an admin override, an administrator's lock).</summary>
    SecurityStampChanged,
}
