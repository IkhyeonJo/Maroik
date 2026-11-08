using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Reads of accounts. Signing in, registering and resetting a password have their own services
/// (<see cref="IAccountLoginService"/>, <see cref="IAccountRegistrationService"/>, <see cref="IAccountPasswordResetService"/>).
/// </summary>
public interface IAccountService
{
    /// <summary>Returns accounts whose nickname matches any of the given values.</summary>
    Task<List<AccountResponse>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default);

    /// <summary>Returns the account with the given email, or null if not found.</summary>
    Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default);
}
