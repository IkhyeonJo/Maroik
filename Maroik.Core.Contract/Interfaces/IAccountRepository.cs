using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IAccountRepository
{
    public Task<List<AccountResponse>> GetAllAsync();
    public Task<AccountResponse?> GetAccountByEmailAsync(string email);
    public Task CreateAccountAsync(AccountRequest request);
    public Task UpdateAccountAsync(AccountRequest request);
}
