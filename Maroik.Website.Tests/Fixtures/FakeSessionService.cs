using Maroik.Core.Contract.Dtos;
using Maroik.Website.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ISessionService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeSessionService : ISessionService
{
    /// <summary>What <c>GetAccountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<AccountResponse?>>? OnGetAccountAsync { get; set; }

    /// <summary>The arguments of every <c>GetAccountAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> GetAccountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<AccountResponse?> GetAccountAsync(CancellationToken ct)
    {
        lock (GetAccountAsyncCalls) GetAccountAsyncCalls.Add(ct);
        if (OnGetAccountAsync is { } on)
        {
            return on(ct);
        }

        return Task.FromResult<AccountResponse?>(default);
    }

    /// <summary>What <c>SetAccount</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<AccountResponse>? OnSetAccount { get; set; }

    /// <summary>The arguments of every <c>SetAccount</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<AccountResponse> SetAccountCalls { get; } = [];

    /// <inheritdoc/>
    public void SetAccount(AccountResponse account)
    {
        lock (SetAccountCalls) SetAccountCalls.Add(account);
        OnSetAccount?.Invoke(account);
    }

    /// <summary>What <c>RemoveAccount</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action? OnRemoveAccount { get; set; }

    /// <summary>How many times <c>RemoveAccount</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int RemoveAccountCallCount => Volatile.Read(ref _removeAccountCallCount);

    /// <summary>Backs <see cref="RemoveAccountCallCount"/>.</summary>
    private int _removeAccountCallCount;

    /// <inheritdoc/>
    public void RemoveAccount()
    {
        Interlocked.Increment(ref _removeAccountCallCount);
        OnRemoveAccount?.Invoke();
    }
}
