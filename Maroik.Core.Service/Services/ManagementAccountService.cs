using ErrorOr;
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
/// Errors are logged and returned to the caller as a typed <see cref="ServiceResult"/>
/// (<see cref="ServiceResult.NotFound"/> / <see cref="ServiceResult.Conflict"/> /
/// <see cref="ServiceResult.Failure"/>).
/// </summary>
public class ManagementAccountService : IManagementAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ManagementAccountService> _logger;

    /// <summary>Initializes a new instance of <see cref="ManagementAccountService"/> with the supplied dependencies.</summary>
    public ManagementAccountService(
        IAccountRepository accountRepository,
        IPasswordService passwordService,
        IUnitOfWork unitOfWork,
        ILogger<ManagementAccountService> logger)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<AdminAccountResponse>> GetAllAccountsAsync(CancellationToken ct = default)
        => (await _accountRepository.GetAllAsync(ct)).Select(AccountMapper.ToAdminResponse).ToList();

    /// <inheritdoc />
    public async Task<List<AdminAccountResponse>> SearchAccountsAsync(string search, CancellationToken ct = default)
        => (await _accountRepository.SearchAsync(search, ct)).Select(AccountMapper.ToAdminResponse).ToList();

    /// <inheritdoc />
    public async Task<AdminAccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default)
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
                return ServiceResult.Conflict("Account.AlreadyExists", "This account has already been created.");

            // request.PlainPassword carries the raw password. The complexity rule is checked here,
            // before hashing, so the controller no longer has to — Account.Create only ever sees the hash.
            if (!PasswordPolicy.IsValid(request.PlainPassword))
                return ServiceResult.Validation("Account.PasswordPolicy", PasswordPolicy.ViolationMessage);

            string hashedPassword = _passwordService.HashPassword(request.PlainPassword ?? "");

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
                return ServiceResult.FromError(createResult.FirstError);

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
            return ServiceResult.Failure("ManagementAccount.CreateFailed", "Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAccountAsync(AccountRequest request, string? newPassword, CancellationToken ct = default)
    {
        // An admin-set password always forces the account to pick its own new one (meeting
        // PasswordPolicy) at next login — see Account.AdminResetPassword. It must still meet the
        // policy itself so there is no weak-password window before that forced change. Hash it up
        // front, outside the transaction, so the row lock below isn't held across the (deliberately
        // slow) BCrypt work.
        string? newHashedPassword = null;
        if (!string.IsNullOrEmpty(newPassword))
        {
            if (!PasswordPolicy.IsValid(newPassword))
                return ServiceResult.Validation("Account.PasswordPolicy", PasswordPolicy.ViolationMessage);

            newHashedPassword = _passwordService.HashPassword(newPassword);
        }

        await _unitOfWork.BeginAsync(ct);
        try
        {
            // FOR UPDATE: hold the row lock from read to commit so a concurrent self-service
            // column-scoped write (profile avatar/timezone/password change) on the same account
            // serializes behind this admin edit instead of being silently reverted by the full-row
            // write below.
            Account? account = await _accountRepository.FindByEmailForUpdateAsync(request.Email ?? "", ct);
            if (account == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.NotFound("Account.NotFound", "Email address is wrong");
            }

            if (newHashedPassword != null)
                account.AdminResetPassword(newHashedPassword);

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
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.FromError(updateResult.FirstError);
            }

            await _accountRepository.UpdateEntityAsync(account, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update account for {Email}", request.Email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("ManagementAccount.UpdateFailed", "Input is invalid");
        }
    }

    private Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
        => _accountRepository.FindByEmailAsync(email, ct);

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAccountAsync(string email, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            // FOR UPDATE, same reason as UpdateAccountAsync: SoftDelete is persisted as a full-row
            // write, so the row must stay locked from read to commit or a concurrent self-service
            // column-scoped change would be reverted.
            Account? account = await _accountRepository.FindByEmailForUpdateAsync(email, ct);
            if (account == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.NotFound("Account.NotFound", "Fail to find the account by given email address");
            }

            account.SoftDelete();

            await _accountRepository.UpdateEntityAsync(account, ct);
            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to delete account for {Email}", email);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Failure("ManagementAccount.DeleteFailed", "Input is invalid");
        }
    }
}
