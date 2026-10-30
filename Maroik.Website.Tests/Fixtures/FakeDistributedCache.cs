using Microsoft.Extensions.Caching.Distributed;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IDistributedCache"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeDistributedCache : IDistributedCache
{
    /// <summary>What <c>Get</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, byte[]?>? OnGet { get; set; }

    /// <summary>The arguments of every <c>Get</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> GetCalls { get; } = [];

    /// <inheritdoc/>
    public byte[]? Get(string key)
    {
        lock (GetCalls) GetCalls.Add(key);
        if (OnGet is { } on)
        {
            return on(key);
        }

        return [];
    }

    /// <summary>What <c>GetAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<byte[]?>>? OnGetAsync { get; set; }

    /// <summary>The arguments of every <c>GetAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Key, CancellationToken Token)> GetAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<byte[]?> GetAsync(string key, CancellationToken token)
    {
        lock (GetAsyncCalls) GetAsyncCalls.Add((key, token));
        if (OnGetAsync is { } on)
        {
            return on(key, token);
        }

        return Task.FromResult<byte[]?>([]);
    }

    /// <summary>What <c>Set</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<string, byte[], DistributedCacheEntryOptions>? OnSet { get; set; }

    /// <summary>The arguments of every <c>Set</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Key, byte[] Value, DistributedCacheEntryOptions Options)> SetCalls { get; } = [];

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        lock (SetCalls) SetCalls.Add((key, value, options));
        OnSet?.Invoke(key, value, options);
    }

    /// <summary>What <c>SetAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, byte[], DistributedCacheEntryOptions, CancellationToken, Task>? OnSetAsync { get; set; }

    /// <summary>The arguments of every <c>SetAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Key, byte[] Value, DistributedCacheEntryOptions Options, CancellationToken Token)> SetAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
    {
        lock (SetAsyncCalls) SetAsyncCalls.Add((key, value, options, token));
        if (OnSetAsync is { } on)
        {
            return on(key, value, options, token);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>Refresh</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<string>? OnRefresh { get; set; }

    /// <summary>The arguments of every <c>Refresh</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> RefreshCalls { get; } = [];

    /// <inheritdoc/>
    public void Refresh(string key)
    {
        lock (RefreshCalls) RefreshCalls.Add(key);
        OnRefresh?.Invoke(key);
    }

    /// <summary>What <c>RefreshAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnRefreshAsync { get; set; }

    /// <summary>The arguments of every <c>RefreshAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Key, CancellationToken Token)> RefreshAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task RefreshAsync(string key, CancellationToken token)
    {
        lock (RefreshAsyncCalls) RefreshAsyncCalls.Add((key, token));
        if (OnRefreshAsync is { } on)
        {
            return on(key, token);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>Remove</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<string>? OnRemove { get; set; }

    /// <summary>The arguments of every <c>Remove</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> RemoveCalls { get; } = [];

    /// <inheritdoc/>
    public void Remove(string key)
    {
        lock (RemoveCalls) RemoveCalls.Add(key);
        OnRemove?.Invoke(key);
    }

    /// <summary>What <c>RemoveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnRemoveAsync { get; set; }

    /// <summary>The arguments of every <c>RemoveAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Key, CancellationToken Token)> RemoveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken token)
    {
        lock (RemoveAsyncCalls) RemoveAsyncCalls.Add((key, token));
        if (OnRemoveAsync is { } on)
        {
            return on(key, token);
        }

        return Task.CompletedTask;
    }
}
