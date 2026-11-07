using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IAccountPolicyService"/>
public sealed class AccountPolicyService : IAccountPolicyService
{
    /// <inheritdoc />
    public bool IsPasswordWithinMaxByteLength(string? password) => PasswordPolicy.IsWithinMaxByteLength(password);

    /// <inheritdoc />
    public string? GetSessionInvalidationReason(AccountResponse? current, AccountResponse session) =>
        AccountSessionPolicy.InvalidationReason(current != null, current?.Deleted ?? false, current?.SecurityStamp, session.SecurityStamp)?.ToString();

    /// <inheritdoc />
    public string GetAccountCreatedMessage() => AccountMessage.Success.GetDescription();

    /// <inheritdoc />
    public bool SeesLocalTime(AccountResponse viewer) => AccountViewPolicy.SeesLocalTime(viewer.Role);

    /// <inheritdoc />
    public HashSet<string> GetAdminNicknames(IEnumerable<AccountResponse> accounts) =>
    [
        .. accounts.Where(account => account.Role == Role.Admin && !string.IsNullOrEmpty(account.Nickname))
            .Select(account => account.Nickname!)
    ];
}
