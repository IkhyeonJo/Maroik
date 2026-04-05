using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IManagementAccountService
{
    Task<List<AccountResponse>> GetAllAccountsAsync();
    Task<AccountResponse?> GetAccountByEmailAsync(string email);
    Task<ServiceResult> CreateAccountAsync(AccountRequest request);
    Task<ServiceResult> UpdateAccountAsync(AccountRequest request, string? newPassword);
    Task<ServiceResult> DeleteAccountAsync(string email);
}
