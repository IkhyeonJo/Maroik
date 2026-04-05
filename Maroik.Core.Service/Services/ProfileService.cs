using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

public class ProfileService : IProfileService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IAccountRepository accountRepository,
        IPasswordService passwordService,
        ILogger<ProfileService> logger)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<ServiceResult> UpdateAvatarAsync(string email, string avatarPath)
    {
        try
        {
            AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            account.AvatarImagePath = avatarPath;
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update avatar for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> UpdateTimeZoneAsync(string email, string timeZoneIanaId)
    {
        try
        {
            AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            account.TimeZoneIanaId = timeZoneIanaId;
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update timezone for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> UpdatePasswordAsync(string email, string currentPassword, string newPassword)
    {
        try
        {
            AccountResponse? account = await _accountRepository.GetAccountByEmailAsync(email);
            if (account == null)
                return ServiceResult.Fail("Input is invalid");

            if (!_passwordService.VerifyPassword(currentPassword, account.HashedPassword ?? ""))
                return ServiceResult.Fail("Invalid password. Please check again.");

            account.HashedPassword = _passwordService.HashPassword(newPassword);
            await _accountRepository.UpdateAccountAsync(account.Adapt<AccountRequest>());
            return ServiceResult.Ok();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to update password for {Email}", email);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
