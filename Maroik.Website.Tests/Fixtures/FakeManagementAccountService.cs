using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IManagementAccountService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeManagementAccountService : IManagementAccountService
{
    /// <summary>What <c>GetAllAccountsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<List<AccountResponse>>>? OnGetAllAccountsAsync { get; set; }

    /// <summary>The arguments of every <c>GetAllAccountsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAllAccountsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<AccountResponse>> GetAllAccountsAsync(CancellationToken ct)
    {
        lock (GetAllAccountsAsyncCalls) GetAllAccountsAsyncCalls.Add(ct);
        if (OnGetAllAccountsAsync is { } on)
        {
            return on(ct);
        }

        return Task.FromResult<List<AccountResponse>>([]);
    }

    /// <summary>What <c>SearchAccountsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<AccountResponse>>>? OnSearchAccountsAsync { get; set; }

    /// <summary>The arguments of every <c>SearchAccountsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Search, CancellationToken Ct)> SearchAccountsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<AccountResponse>> SearchAccountsAsync(string search, CancellationToken ct)
    {
        lock (SearchAccountsAsyncCalls) SearchAccountsAsyncCalls.Add((search, ct));
        if (OnSearchAccountsAsync is { } on)
        {
            return on(search, ct);
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

    /// <summary>What <c>CreateAccountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AdminCreateAccountRequest, string, CancellationToken, Task<ServiceResult>>? OnCreateAccountAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAccountAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(AdminCreateAccountRequest Request, string ActorEmail, CancellationToken Ct)> CreateAccountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CreateAccountAsync(AdminCreateAccountRequest request, string actorEmail, CancellationToken ct)
    {
        lock (CreateAccountAsyncCalls) CreateAccountAsyncCalls.Add((request, actorEmail, ct));
        if (OnCreateAccountAsync is { } on)
        {
            return on(request, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>UpdateAccountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<AdminUpdateAccountRequest, string?, string, CancellationToken, Task<ServiceResult>>? OnUpdateAccountAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateAccountAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(AdminUpdateAccountRequest Request, string? NewPassword, string ActorEmail, CancellationToken Ct)> UpdateAccountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> UpdateAccountAsync(AdminUpdateAccountRequest request, string? newPassword, string actorEmail, CancellationToken ct)
    {
        lock (UpdateAccountAsyncCalls) UpdateAccountAsyncCalls.Add((request, newPassword, actorEmail, ct));
        if (OnUpdateAccountAsync is { } on)
        {
            return on(request, newPassword, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteAccountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<ServiceResult>>? OnDeleteAccountAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteAccountAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, string ActorEmail, CancellationToken Ct)> DeleteAccountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteAccountAsync(string email, string actorEmail, CancellationToken ct)
    {
        lock (DeleteAccountAsyncCalls) DeleteAccountAsyncCalls.Add((email, actorEmail, ct));
        if (OnDeleteAccountAsync is { } on)
        {
            return on(email, actorEmail, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }
}
