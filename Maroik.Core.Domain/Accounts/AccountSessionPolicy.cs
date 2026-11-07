namespace Maroik.Core.Domain.Accounts;

/// <summary>
/// Whether a signed-in session still stands for its account. The session captured the account by value at login, so the
/// account's current row decides on every request: a deleted or missing account, or a security stamp that moved on (a password
/// change or reset, an admin override, an administrator's lock), ends the session. A lock from failed logins replaces no stamp and
/// so ends nothing: it only refuses new logins, or anyone who knows the address could throw its owner out by guessing wrong.
/// The one definition of the rule for both gates that apply it, <c>AuthorizationFilter</c> and <c>RoleBasedStaticFileMiddleware</c>.
/// </summary>
public static class AccountSessionPolicy
{
    /// <summary>Why the session no longer stands for its account, for the log, or <see langword="null"/> while it still does.</summary>
    /// <param name="accountExists">Whether the session's account still has a row.</param>
    /// <param name="deleted">Whether that account is soft-deleted.</param>
    /// <param name="currentStamp">The account's current security stamp.</param>
    /// <param name="sessionStamp">The security stamp the session captured at login.</param>
    public static SessionInvalidationReason? InvalidationReason(bool accountExists, bool deleted, string? currentStamp, string? sessionStamp)
    {
        if (!accountExists) return SessionInvalidationReason.AccountMissing;
        if (deleted) return SessionInvalidationReason.AccountDeleted;
        return currentStamp == sessionStamp ? null : SessionInvalidationReason.SecurityStampChanged;
    }
}
