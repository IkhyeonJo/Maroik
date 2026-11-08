using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <summary>Implementation of <see cref="IAccountService"/>: reads of accounts.</summary>
public class AccountService(IAccountRepository accountRepository) : IAccountService
{
    /// <inheritdoc />
    public async Task<List<AccountResponse>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default)
        => [.. (await accountRepository.FindByNicknamesAsync(nicknames, ct)).Select(AccountMapper.ToResponse)];

    /// <inheritdoc />
    public async Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default)
    {
        Account? account = await accountRepository.FindByEmailAsync(email, ct);
        return account == null ? null : AccountMapper.ToResponse(account);
    }
}
