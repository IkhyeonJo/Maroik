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

        // Column-scoped write (Message/Updated only): this runs from the worker and must not
        // full-row-overwrite an Account the web app is editing at the same moment.
        await _accountRepository.UpdateMessageAsync(
            email, EnumHelper.GetDescription(AccountMessage.FailToMailSent), DateTime.UtcNow, ct);
    }

    /// <inheritdoc />
    public async Task ClearMailSendFailedAsync(string email, CancellationToken ct = default)
    {
        Account? account = await _accountRepository.FindByEmailAsync(email, ct);
        if (account == null)
            return;

        if (account.Message != EnumHelper.GetDescription(AccountMessage.FailToMailSent))
            return;

        await _accountRepository.UpdateMessageAsync(email, null, DateTime.UtcNow, ct);
    }
}
