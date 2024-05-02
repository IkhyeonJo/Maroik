using Maroik.Common.DataAccess.Contracts;
using Maroik.WebAPI.Extensions;
using Maroik.WebAPI.Utilities;
using System.Text;

namespace Maroik.WebAPI.Services
{
    public interface IUserService
    {
        Task<bool> IsValidUserCredentials(string email, string password);
    }

    public class UserService : IUserService
    {
        private readonly ILogger<UserService> logger;
        private readonly IAccountRepository accountRepository;

        public UserService(ILogger<UserService> logger, IAccountRepository accountRepository)
        {
            this.logger = logger;
            this.accountRepository = accountRepository;
        }

        public async Task<bool> IsValidUserCredentials(string email, string password)
        {
            logger.LogInformation($"Validating user [{email}]");
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            Maroik.Common.DataAccess.Models.Account tempAccount = await accountRepository.GetAccountByEmailAsync(email);

            if (tempAccount == null)
            {
                return false;
            }

            if (tempAccount.Deleted)
            {
                return false;
            }

            if (tempAccount.Locked)
            {
                return false;
            }

            if (!new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Verify(password ?? "", tempAccount.HashedPassword)) // wrong password
            {
                tempAccount.LoginAttempt++;

                if (tempAccount.LoginAttempt == ServerSetting.MaxLoginAttempt)
                {
                    tempAccount.Locked = true;
                    tempAccount.Message = EnumHelper.GetDescription(AccountMessage.AccountLocked);
                    await accountRepository.UpdateAccountAsync(tempAccount);
                    return false;
                }
                else
                {
                    await accountRepository.UpdateAccountAsync(tempAccount);
                    return false;
                }
            }

            if (tempAccount.EmailConfirmed == true && tempAccount.AgreedServiceTerms == true)
            {
                tempAccount.LoginAttempt = 0;
                await accountRepository.UpdateAccountAsync(tempAccount);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}