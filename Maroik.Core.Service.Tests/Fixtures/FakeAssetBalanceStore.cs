using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAssetBalanceStore"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAssetBalanceStore : IAssetBalanceStore
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetAssetAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<Asset?>>? OnGetAssetAsync { get; set; }

    /// <summary>The arguments of every <c>GetAssetAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string ProductName, CancellationToken Ct)> GetAssetAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAssetAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<Asset?>> Answer)> _getAssetAsyncRules = [];

    /// <summary>Makes every later <c>GetAssetAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetAsync(Func<string, string, CancellationToken, Task<Asset?>> answer) => AnswerGetAssetAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>GetAssetAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<Asset?>> answer)
    {
        lock (_getAssetAsyncRules) _getAssetAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Asset?> GetAssetAsync(string accountEmail, string productName, CancellationToken ct)
    {
        lock (GetAssetAsyncCalls) GetAssetAsyncCalls.Add((accountEmail, productName, ct));
        Func<string, string, CancellationToken, Task<Asset?>>? answer = null;
        lock (_getAssetAsyncRules) answer = _getAssetAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productName, ct)).Answer;
        if ((answer ?? OnGetAssetAsync) is { } on)
        {
            return on(accountEmail, productName, ct);
        }

        return Strict ? throw new InvalidOperationException("GetAssetAsync was called without an answer on a strict fake.") : Task.FromResult<Asset?>(default);
    }

    /// <summary>What <c>GetAssetsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IReadOnlyCollection<string>, CancellationToken, Task<Dictionary<string, Asset>>>? OnGetAssetsAsync { get; set; }

    /// <summary>The arguments of every <c>GetAssetsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, IReadOnlyCollection<string> ProductNames, CancellationToken Ct)> GetAssetsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAssetsAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, IReadOnlyCollection<string>, CancellationToken, bool> Match, Func<string, IReadOnlyCollection<string>, CancellationToken, Task<Dictionary<string, Asset>>> Answer)> _getAssetsAsyncRules = [];

    /// <summary>Makes every later <c>GetAssetsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetsAsync(Func<string, IReadOnlyCollection<string>, CancellationToken, Task<Dictionary<string, Asset>>> answer) => AnswerGetAssetsAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>GetAssetsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetsAsync(Func<string, IReadOnlyCollection<string>, CancellationToken, bool> match, Func<string, IReadOnlyCollection<string>, CancellationToken, Task<Dictionary<string, Asset>>> answer)
    {
        lock (_getAssetsAsyncRules) _getAssetsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Dictionary<string, Asset>> GetAssetsAsync(string accountEmail, IReadOnlyCollection<string> productNames, CancellationToken ct)
    {
        lock (GetAssetsAsyncCalls) GetAssetsAsyncCalls.Add((accountEmail, productNames, ct));
        Func<string, IReadOnlyCollection<string>, CancellationToken, Task<Dictionary<string, Asset>>>? answer = null;
        lock (_getAssetsAsyncRules) answer = _getAssetsAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productNames, ct)).Answer;
        if ((answer ?? OnGetAssetsAsync) is { } on)
        {
            return on(accountEmail, productNames, ct);
        }

        return Strict ? throw new InvalidOperationException("GetAssetsAsync was called without an answer on a strict fake.") : Task.FromResult<Dictionary<string, Asset>>([]);
    }

    /// <summary>What <c>GetAssetForReadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<Asset?>>? OnGetAssetForReadAsync { get; set; }

    /// <summary>The arguments of every <c>GetAssetForReadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string ProductName, CancellationToken Ct)> GetAssetForReadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetAssetForReadAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<Asset?>> Answer)> _getAssetForReadAsyncRules = [];

    /// <summary>Makes every later <c>GetAssetForReadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetForReadAsync(Func<string, string, CancellationToken, Task<Asset?>> answer) => AnswerGetAssetForReadAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>GetAssetForReadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetAssetForReadAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<Asset?>> answer)
    {
        lock (_getAssetForReadAsyncRules) _getAssetForReadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Asset?> GetAssetForReadAsync(string accountEmail, string productName, CancellationToken ct)
    {
        lock (GetAssetForReadAsyncCalls) GetAssetForReadAsyncCalls.Add((accountEmail, productName, ct));
        Func<string, string, CancellationToken, Task<Asset?>>? answer = null;
        lock (_getAssetForReadAsyncRules) answer = _getAssetForReadAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productName, ct)).Answer;
        if ((answer ?? OnGetAssetForReadAsync) is { } on)
        {
            return on(accountEmail, productName, ct);
        }

        return Strict ? throw new InvalidOperationException("GetAssetForReadAsync was called without an answer on a strict fake.") : Task.FromResult<Asset?>(default);
    }

    /// <summary>What <c>SaveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Asset, CancellationToken, Task>? OnSaveAsync { get; set; }

    /// <summary>The arguments of every <c>SaveAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Asset Asset, CancellationToken Ct)> SaveAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerSaveAsync</c> added, newest last.</summary>
    private readonly List<(Func<Asset, CancellationToken, bool> Match, Func<Asset, CancellationToken, Task> Answer)> _saveAsyncRules = [];

    /// <summary>Makes every later <c>SaveAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSaveAsync(Func<Asset, CancellationToken, Task> answer) => AnswerSaveAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>SaveAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSaveAsync(Func<Asset, CancellationToken, bool> match, Func<Asset, CancellationToken, Task> answer)
    {
        lock (_saveAsyncRules) _saveAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task SaveAsync(Asset asset, CancellationToken ct)
    {
        lock (SaveAsyncCalls) SaveAsyncCalls.Add((asset, ct));
        Func<Asset, CancellationToken, Task>? answer = null;
        lock (_saveAsyncRules) answer = _saveAsyncRules.LastOrDefault(rule => rule.Match(asset, ct)).Answer;
        if ((answer ?? OnSaveAsync) is { } on)
        {
            return on(asset, ct);
        }

        return Strict ? throw new InvalidOperationException("SaveAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetAssetAsyncCalls.Count + GetAssetsAsyncCalls.Count + GetAssetForReadAsyncCalls.Count + SaveAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetAssetAsyncCalls) GetAssetAsyncCalls.Clear();
        lock (GetAssetsAsyncCalls) GetAssetsAsyncCalls.Clear();
        lock (GetAssetForReadAsyncCalls) GetAssetForReadAsyncCalls.Clear();
        lock (SaveAsyncCalls) SaveAsyncCalls.Clear();
    }
}
