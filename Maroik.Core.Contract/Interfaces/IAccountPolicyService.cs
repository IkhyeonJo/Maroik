using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The account rules the Website needs; each member answers from a Domain policy.</summary>
public interface IAccountPolicyService
{
    /// <summary>Whether a password fits the longest one the hash can take.</summary>
    bool IsPasswordWithinMaxByteLength(string? password);

    /// <summary>Why a session no longer holds, or null while it does. <paramref name="current"/> is the account as stored now.</summary>
    string? GetSessionInvalidationReason(AccountResponse? current, AccountResponse session);

    /// <summary>Whether the viewer sees local times rather than raw UTC.</summary>
    bool SeesLocalTime(AccountResponse viewer);

    /// <summary>The nicknames of the administrators among the accounts.</summary>
    HashSet<string> GetAdminNicknames(IEnumerable<AccountResponse> accounts);
}
