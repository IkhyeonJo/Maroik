using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

public class ManagementAccountService : IManagementAccountService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordService _passwordService;

    public ManagementAccountService(IAccountRepository accountRepository, IPasswordService passwordService)
    {
        _accountRepository = accountRepository;
        _passwordService = passwordService;
    }

    public async Task<ServiceResult> CreateAccountAsync(AccountRequest request)
    {
        try
        {
            if (await _accountRepository.GetAccountByEmailAsync(request.Email ?? "") != null)
                return ServiceResult.Fail("This account has already been created.");

            request.HashedPassword = _passwordService.HashPassword(request.HashedPassword ?? "");
            await _accountRepository.CreateAccountAsync(request);
            return ServiceResult.Ok();
        }
        catch
        {
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> UpdateAccountAsync(AccountRequest request, string? newPassword)
    {
        try
        {
            AccountResponse? existing = await _accountRepository.GetAccountByEmailAsync(request.Email ?? "");
            if (existing == null)
                return ServiceResult.Fail("Email address is wrong");

            existing.HashedPassword = string.IsNullOrEmpty(newPassword)
                ? existing.HashedPassword
                : _passwordService.HashPassword(newPassword);
            existing.Role = request.Role;
            existing.TimeZoneIanaId = request.TimeZoneIanaId;
            existing.Locked = request.Locked;
            if (!request.Locked)
                existing.LoginAttempt = 0;
            existing.EmailConfirmed = request.EmailConfirmed;
            existing.AgreedServiceTerms = request.AgreedServiceTerms;
            existing.Updated = DateTime.UtcNow;
            existing.Message = request.Message;
            existing.Deleted = request.Deleted;

            await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
            return ServiceResult.Ok();
        }
        catch
        {
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> DeleteAccountAsync(string email)
    {
        try
        {
            AccountResponse? existing = await _accountRepository.GetAccountByEmailAsync(email);
            if (existing == null)
                return ServiceResult.Fail("Fail to find the account by given email address");

            existing.Deleted = true;
            await _accountRepository.UpdateAccountAsync(existing.Adapt<AccountRequest>());
            return ServiceResult.Ok();
        }
        catch
        {
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
