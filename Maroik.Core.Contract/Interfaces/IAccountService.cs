using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IAccountService
{
    Task<List<AccountResponse>> GetAllAccountsAsync();
    Task<AccountResponse?> GetAccountByEmailAsync(string email);
    Task<LoginResult> LoginAsync(string email, string password);
    Task<RegisterResult> RegisterAsync(AccountRequest newAccount, string emailSubject, string emailTitle, string emailContent0, string emailContent1);
    Task<RegisterResult> ResendConfirmationEmailAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1);
    Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken);
    Task<bool> ForgotPasswordAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1);
    Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken);
    Task<ServiceResult> ResetPasswordAsync(string resetPasswordToken, string newPassword);
}
