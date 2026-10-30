using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAccountService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAccountService : IAccountService
{
    /// <summary>What <c>GetAccountsByNicknamesAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<string>, CancellationToken, Task<List<AccountResponse>>>? OnGetAccountsByNicknamesAsync { get; set; }

    /// <summary>The arguments of every <c>GetAccountsByNicknamesAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<string> Nicknames, CancellationToken Ct)> GetAccountsByNicknamesAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<AccountResponse>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct)
    {
        lock (GetAccountsByNicknamesAsyncCalls) GetAccountsByNicknamesAsyncCalls.Add((nicknames, ct));
        if (OnGetAccountsByNicknamesAsync is { } on)
        {
            return on(nicknames, ct);
        }

        return Task.FromResult<List<AccountResponse>>([]);
    }

    /// <summary>What <c>GetAccountByEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<AccountResponse?>>? OnGetAccountByEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetAccountByEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> GetAccountByEmailAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct)
    {
        lock (GetAccountByEmailAsyncCalls) GetAccountByEmailAsyncCalls.Add((email, ct));
        if (OnGetAccountByEmailAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.FromResult<AccountResponse?>(default);
    }

    /// <summary>What <c>LoginAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<LoginResult>>? OnLoginAsync { get; set; }

    /// <summary>The arguments of every <c>LoginAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string Password, CancellationToken Ct)> LoginAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct)
    {
        lock (LoginAsyncCalls) LoginAsyncCalls.Add((email, password, ct));
        if (OnLoginAsync is { } on)
        {
            return on(email, password, ct);
        }

        return Task.FromResult<LoginResult>(default!);
    }

    /// <summary>What <c>RegisterAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<RegisterAccountRequest, EmailTemplate, CancellationToken, Task<RegisterResult>>? OnRegisterAsync { get; set; }

    /// <summary>The arguments of every <c>RegisterAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(RegisterAccountRequest NewAccount, EmailTemplate EmailTemplate, CancellationToken Ct)> RegisterAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<RegisterResult> RegisterAsync(RegisterAccountRequest newAccount, EmailTemplate emailTemplate, CancellationToken ct)
    {
        lock (RegisterAsyncCalls) RegisterAsyncCalls.Add((newAccount, emailTemplate, ct));
        if (OnRegisterAsync is { } on)
        {
            return on(newAccount, emailTemplate, ct);
        }

        return Task.FromResult<RegisterResult>(default!);
    }

    /// <summary>What <c>ResendConfirmationEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, EmailTemplate, CancellationToken, Task<RegisterResult>>? OnResendConfirmationEmailAsync { get; set; }

    /// <summary>The arguments of every <c>ResendConfirmationEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, EmailTemplate EmailTemplate, CancellationToken Ct)> ResendConfirmationEmailAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<RegisterResult> ResendConfirmationEmailAsync(string email, EmailTemplate emailTemplate, CancellationToken ct)
    {
        lock (ResendConfirmationEmailAsyncCalls) ResendConfirmationEmailAsyncCalls.Add((email, emailTemplate, ct));
        if (OnResendConfirmationEmailAsync is { } on)
        {
            return on(email, emailTemplate, ct);
        }

        return Task.FromResult<RegisterResult>(default!);
    }

    /// <summary>What <c>ValidateRegistrationTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<ConfirmEmailResult>>? OnValidateRegistrationTokenAsync { get; set; }

    /// <summary>The arguments of every <c>ValidateRegistrationTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string RegistrationToken, CancellationToken Ct)> ValidateRegistrationTokenAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ConfirmEmailResult> ValidateRegistrationTokenAsync(string registrationToken, CancellationToken ct)
    {
        lock (ValidateRegistrationTokenAsyncCalls) ValidateRegistrationTokenAsyncCalls.Add((registrationToken, ct));
        if (OnValidateRegistrationTokenAsync is { } on)
        {
            return on(registrationToken, ct);
        }

        return Task.FromResult<ConfirmEmailResult>(default!);
    }

    /// <summary>What <c>ConfirmEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<ConfirmEmailResult>>? OnConfirmEmailAsync { get; set; }

    /// <summary>The arguments of every <c>ConfirmEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string RegistrationToken, string Password, CancellationToken Ct)> ConfirmEmailAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken, string password, CancellationToken ct)
    {
        lock (ConfirmEmailAsyncCalls) ConfirmEmailAsyncCalls.Add((registrationToken, password, ct));
        if (OnConfirmEmailAsync is { } on)
        {
            return on(registrationToken, password, ct);
        }

        return Task.FromResult<ConfirmEmailResult>(default!);
    }

    /// <summary>What <c>ForgotPasswordAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, EmailTemplate, CancellationToken, Task<bool>>? OnForgotPasswordAsync { get; set; }

    /// <summary>The arguments of every <c>ForgotPasswordAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, EmailTemplate EmailTemplate, CancellationToken Ct)> ForgotPasswordAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<bool> ForgotPasswordAsync(string email, EmailTemplate emailTemplate, CancellationToken ct)
    {
        lock (ForgotPasswordAsyncCalls) ForgotPasswordAsyncCalls.Add((email, emailTemplate, ct));
        if (OnForgotPasswordAsync is { } on)
        {
            return on(email, emailTemplate, ct);
        }

        return Task.FromResult<bool>(default);
    }

    /// <summary>What <c>ValidateResetPasswordTokenAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<ResetPasswordValidationResult>>? OnValidateResetPasswordTokenAsync { get; set; }

    /// <summary>The arguments of every <c>ValidateResetPasswordTokenAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string ResetPasswordToken, CancellationToken Ct)> ValidateResetPasswordTokenAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken, CancellationToken ct)
    {
        lock (ValidateResetPasswordTokenAsyncCalls) ValidateResetPasswordTokenAsyncCalls.Add((resetPasswordToken, ct));
        if (OnValidateResetPasswordTokenAsync is { } on)
        {
            return on(resetPasswordToken, ct);
        }

        return Task.FromResult<ResetPasswordValidationResult>(default!);
    }

    /// <summary>What <c>ResetPasswordAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<(ServiceResult, AccountResponse?)>>? OnResetPasswordAsync { get; set; }

    /// <summary>The arguments of every <c>ResetPasswordAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string ResetPasswordToken, string NewPassword, CancellationToken Ct)> ResetPasswordAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, AccountResponse?)> ResetPasswordAsync(string resetPasswordToken, string newPassword, CancellationToken ct)
    {
        lock (ResetPasswordAsyncCalls) ResetPasswordAsyncCalls.Add((resetPasswordToken, newPassword, ct));
        if (OnResetPasswordAsync is { } on)
        {
            return on(resetPasswordToken, newPassword, ct);
        }

        return Task.FromResult<(ServiceResult, AccountResponse?)>(default);
    }
}
