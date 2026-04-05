using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IAccountService
{
    Task<List<AccountResponse>> GetAllAccountsAsync();
    Task<LoginResult> LoginAsync(string email, string password);
    Task<RegisterResult> RegisterAsync(AccountRequest newAccount, string emailSubject, Func<string, string> emailBodyFactory);
    Task<RegisterResult> ResendConfirmationEmailAsync(string email, string emailSubject, Func<string, string> emailBodyFactory);
    Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken);
    Task<bool> ForgotPasswordAsync(string email, string emailSubject, Func<string, string> emailBodyFactory);
    Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken);
    Task<ServiceResult> ResetPasswordAsync(string resetPasswordToken, string newPassword);
}
