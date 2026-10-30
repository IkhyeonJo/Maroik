using Maroik.Core.Contract.Extensions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Time.Testing;
// ReSharper disable AccessToDisposedClosure

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AccountMailStatusService"/>.
/// <see cref="IAccountRepository"/> is replaced with a hand-written fake so tests run without a database.
/// </summary>
public class AccountMailStatusServiceTests
{
    /// <summary>Fake <c>IAccountRepository</c> injected into the system under test.</summary>
    private readonly FakeAccountRepository _accountRepo = new();

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake repository.</summary>
    private AccountMailStatusService CreateSut() => new(_accountRepo, _time);

    /// <summary>A confirmed, unlocked account whose stored mail-status message is <paramref name="message"/>.</summary>
    private static Account ActiveAccount(string email = "user@example.com", string? message = null) => Account.Reconstitute(
        email: email,
        hashedPassword: "$2a$13$placeholder",
        nickname: "User",
        avatarImagePath: null,
        role: Role.User,
        timeZoneIanaId: "UTC",
        defaultMonetaryUnit: null,
        locked: false,
        loginAttempt: 0,
        emailConfirmed: true,
        agreedServiceTerms: true,
        registrationToken: null,
        resetPasswordToken: null,
        created: DateTime.UtcNow,
        updated: DateTime.UtcNow,
        message: message,
        deleted: false,
        securityStamp: "stamp",
        mustChangePassword: false);

    /// <summary>Verifies that <c>MarkMailSendFailedAsync</c> sets the FailToMailSent message and persists it.</summary>
    [Fact]
    public async Task MarkMailSendFailedAsync_SetsFailToMailSentMessage_WhenAccountFound()
    {
        var account = ActiveAccount();
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        await sut.MarkMailSendFailedAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Message == AccountMessage.FailToMailSent.GetDescription());
    }

    /// <summary>Verifies that <c>MarkMailSendFailedAsync</c> does nothing when no matching account exists.</summary>
    [Fact]
    public async Task MarkMailSendFailedAsync_DoesNothing_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        await sut.MarkMailSendFailedAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(_accountRepo.UpdateMessageAsyncCalls);
    }

    /// <summary>Passes cancellation token through to the repository.</summary>
    [Fact]
    public async Task MarkMailSendFailedAsync_PassesCancellationTokenThroughToRepository()
    {
        var account = ActiveAccount();
        using var cts = new CancellationTokenSource();
        _accountRepo.AnswerFindByEmailAsync((email, ct) => email == account.Email.Value && ct == cts.Token, (_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        await sut.MarkMailSendFailedAsync(account.Email.Value, cts.Token);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Ct == cts.Token);
    }

    /// <summary>Verifies that <c>ClearMailSendFailedAsync</c> clears a FailToMailSent message and persists it.</summary>
    [Fact]
    public async Task ClearMailSendFailedAsync_ClearsMessage_WhenAccountWasFailToMailSent()
    {
        var account = ActiveAccount(message: AccountMessage.FailToMailSent.GetDescription());
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        await sut.ClearMailSendFailedAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.Single(_accountRepo.UpdateMessageAsyncCalls, call => call.Email == account.Email.Value && call.Message == null);
    }

    /// <summary>Verifies that <c>ClearMailSendFailedAsync</c> does nothing when no matching account exists.</summary>
    [Fact]
    public async Task ClearMailSendFailedAsync_DoesNothing_WhenAccountNotFound()
    {
        _accountRepo.AnswerFindByEmailAsync((_, _) => Task.FromResult<Account?>(null));
        var sut = CreateSut();

        await sut.ClearMailSendFailedAsync("ghost@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(_accountRepo.UpdateMessageAsyncCalls);
    }

    /// <summary>Verifies that <c>ClearMailSendFailedAsync</c> leaves an unrelated message untouched.</summary>
    [Fact]
    public async Task ClearMailSendFailedAsync_DoesNothing_WhenAccountMessageIsNotFailToMailSent()
    {
        var account = ActiveAccount(message: AccountMessage.ResetPasswordMail.GetDescription());
        _accountRepo.AnswerFindByEmailAsync((email, _) => email == account.Email.Value, (_, _) => Task.FromResult<Account?>(account));
        var sut = CreateSut();

        await sut.ClearMailSendFailedAsync(account.Email.Value, TestContext.Current.CancellationToken);

        Assert.Empty(_accountRepo.UpdateMessageAsyncCalls);
    }
}
