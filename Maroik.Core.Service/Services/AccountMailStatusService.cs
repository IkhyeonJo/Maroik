using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IAccountMailStatusService"/>
public class AccountMailStatusService : IAccountMailStatusService
{
    private readonly IAccountRepository _accountRepository;

    /// <summary>Initializes a new instance of <see cref="AccountMailStatusService"/> with the supplied dependencies.</summary>
    public AccountMailStatusService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    /// <inheritdoc />
    public async Task MarkMailSendFailedAsync(string email, CancellationToken ct = default)
    {
        Account? account = await _accountRepository.FindByEmailAsync(email, ct);
        if (account == null)
            return;

        account.SetMessage(EnumHelper.GetDescription(AccountMessage.FailToMailSent));
        await _accountRepository.UpdateEntityAsync(account, ct);
    }
}
