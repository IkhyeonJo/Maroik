using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ISessionService
{
    AccountResponse? GetAccount();
    void SetAccount(AccountResponse account);
    void RemoveAccount();
}
