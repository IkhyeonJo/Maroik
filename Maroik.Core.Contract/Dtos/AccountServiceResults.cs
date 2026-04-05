namespace Maroik.Core.Contract.Dtos;

public class LoginResult
{
    public bool Success { get; init; }
    public string ErrorKey { get; init; } = string.Empty;
    public AccountResponse? Account { get; init; }

    public static LoginResult Ok(AccountResponse account) => new() { Success = true, Account = account };
    public static LoginResult Fail(string errorKey) => new() { Success = false, ErrorKey = errorKey };
}

public class RegisterResult
{
    public bool Success { get; init; }
    public string ErrorKey { get; init; } = string.Empty;
    public bool ShowResendEmail { get; init; }
    public string? EmailAddress { get; init; }
    public bool RepeatEmailSend { get; init; }

    public static RegisterResult Ok(bool showResendEmail = false, string? email = null, bool repeat = false)
        => new() { Success = true, ShowResendEmail = showResendEmail, EmailAddress = email, RepeatEmailSend = repeat };

    public static RegisterResult Fail(string errorKey, bool showResendEmail = false, string? email = null)
        => new() { Success = false, ErrorKey = errorKey, ShowResendEmail = showResendEmail, EmailAddress = email };
}

public class ConfirmEmailResult
{
    public bool InvalidToken { get; init; }
    public bool AccountCreated { get; init; }
    public string ErrorKey { get; init; } = string.Empty;
}

public class ResetPasswordValidationResult
{
    public bool FailToReset { get; init; }
    public string? ResetPasswordToken { get; init; }
}
