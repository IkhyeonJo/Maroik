using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IManagementAccountService"/> for admin account CRUD operations.
/// Passwords supplied in create/update operations are hashed via <see cref="IPasswordService"/>
/// before being stored. Soft-deleted accounts can be restored by the admin.
/// Errors are logged and returned as <see cref="ServiceResult.Fail"/> to the caller.
/// </summary>
public class ManagementAccountService : IManagementAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ManagementAccountService> _logger;

    /// <summary>Initializes a new instance of <see cref="ManagementAccountService"/> with the supplied dependencies.</summary>
    public ManagementAccountService(
        IAccountRepository accountRepository,
        IPasswordService passwordService,
        ILogger<ManagementAccountService> logger)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<AccountResponse>> GetAllAccountsAsync(CancellationToken ct = default)
        => (await _accountRepository.GetAllAsync(ct)).Select(AccountMapper.ToAdminResponse).ToList();

    /// <inheritdoc />
    public async Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default)
    {
        Account? account = await FindByEmailAsync(email, ct);
        return account == null ? null : AccountMapper.ToAdminResponse(account);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAccountAsync(AccountRequest request, CancellationToken ct = default)
    {
        try
        {
            if (await FindByEmailAsync(request.Email ?? "", ct) != null)
                return ServiceResult.Fail("This account has already been created.");

            string hashedPassword = _passwordService.HashPassword(request.HashedPassword ?? "");

            var createResult = Account.Create(
                request.Email ?? "",
                hashedPassword,
                request.Nickname ?? "",
                request.Role ?? Role.User,
                request.TimeZoneIanaId ?? "UTC",
                request.DefaultMonetaryUnit,
                request.RegistrationToken,
                request.AgreedServiceTerms);

            if (createResult.IsError)
                return ServiceResult.Fail(createResult.FirstError.Description);

            var account = createResult.Value;
            account.SetMessage(request.Message);

            if (request.EmailConfirmed)
                account.ForceConfirmEmail();

            await _accountRepository.CreateAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to create account for {Email}", request.Email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAccountAsync(AccountRequest request, string? newPassword, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(request.Email ?? "", ct);
            if (account == null)
                return ServiceResult.Fail("Email address is wrong");

            if (!string.IsNullOrEmpty(newPassword))
                account.ChangePassword(_passwordService.HashPassword(newPassword));

            // The admin UI has no LoginAttempt input, so request.LoginAttempt is always the request
            // DTO's default (0) — pass the account's own current value instead, so AdminUpdate's
            // "preserve while locked, reset on unlock" logic has a real count to preserve.
            var updateResult = account.AdminUpdate(
                request.Role,
                request.TimeZoneIanaId,
                request.Locked,
                account.LoginAttempt,
                request.EmailConfirmed,
                request.AgreedServiceTerms,
                request.Message,
                request.Deleted);

            if (updateResult.IsError)
                return ServiceResult.Fail(updateResult.FirstError.Description);

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update account for {Email}", request.Email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    private Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _accountRepository.FindByEmailAsync(email, ct);

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAccountAsync(string email, CancellationToken ct = default)
    {
        try
        {
            Account? account = await FindByEmailAsync(email, ct);
            if (account == null)
                return ServiceResult.Fail("Fail to find the account by given email address");

            account.SoftDelete();

            await _accountRepository.UpdateEntityAsync(account, ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete account for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
