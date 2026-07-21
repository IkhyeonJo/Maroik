using Maroik.Core.Contract.Dtos;

namespace Maroik.Website.Contracts;

/// <summary>
/// Service interface for reading and writing the current user's account data
/// in the server-side session store.
/// </summary>
public interface ISessionService
{
    /// <summary>Reads the currently logged-in account from the session. Returns null if not authenticated.</summary>
    AccountResponse? GetAccount();

    /// <summary>Serializes the account into the session (called after a successful login).</summary>
    void SetAccount(AccountResponse account);

    /// <summary>Clears the account from the session (called on logout).</summary>
    void RemoveAccount();

    /// <summary>
    /// Replaces the current session with a brand-new one under a freshly generated ID, discarding
    /// any data (and cookie) tied to the previous session. Must be called on successful login,
    /// before <see cref="SetAccount"/>, so a session ID that existed prior to authentication can
    /// never be reused post-authentication (defends against session fixation).
    /// </summary>
    void RegenerateSessionId();
}
