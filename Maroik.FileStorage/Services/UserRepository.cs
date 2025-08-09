using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.FileStorage.Contracts;
using System.Text;

namespace Maroik.FileStorage.Services;

public class UserRepository(ILogger<UserRepository> logger, IAccountRepository accountRepository)
    : IUserRepository
{
    private readonly Rsa _rsa = new(RsaType.Rsa2, Encoding.UTF8, Rsa.PrivateKey, Rsa.PublicKey);

    public async Task<bool> IsValidUserCredentialsAsync(string email, string password)
    {
        logger.LogInformation("Validating user [{Email}]", email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var tempAccount = await accountRepository.GetAccountByEmailAsync(email);

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

        if (!_rsa.Verify(password, tempAccount.HashedPassword)) // wrong password
        {
            tempAccount.LoginAttempt++;

            if (tempAccount.LoginAttempt == ServerSetting.MaxLoginAttempt)
            {
                tempAccount.Locked = true;
                tempAccount.Message = EnumHelper.GetDescription(AccountMessage.AccountLocked);
            }

            await accountRepository.UpdateAccountAsync(tempAccount);
            return false;
        }

        if (tempAccount.EmailConfirmed != true || tempAccount.AgreedServiceTerms != true) return false;
        tempAccount.LoginAttempt = 0;
        await accountRepository.UpdateAccountAsync(tempAccount);
        return true;

    }
}