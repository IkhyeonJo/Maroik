using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

public class ProfileService : IProfileService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;

    public ProfileService(IAccountRepository accountRepository, IPasswordService passwordService)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
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
        catch
        {
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
        catch
        {
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
        catch
        {
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
