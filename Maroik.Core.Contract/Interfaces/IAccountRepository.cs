using Maroik.Core.Contract.Domains.Account;
namespace Maroik.Core.Contract.Interfaces;
public interface IAccountRepository : IGenericRepository<Account>
{
    Task<List<Account>> GetAllAsync(CancellationToken ct = default);
    Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<Account?> FindByRegistrationTokenAsync(string token, CancellationToken ct = default);
    Task<Account?> FindByResetPasswordTokenAsync(string token, CancellationToken ct = default);
    Task<List<Account>> FindByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default);
}
