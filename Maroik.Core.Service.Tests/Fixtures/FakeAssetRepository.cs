using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAssetRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAssetRepository : IAssetRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>UpdateAssetWithProductNameAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Asset, string, CancellationToken, Task<int>>? OnUpdateAssetWithProductNameAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateAssetWithProductNameAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Asset Asset, string OriginalProductName, CancellationToken Ct)> UpdateAssetWithProductNameAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateAssetWithProductNameAsync</c> added, newest last.</summary>
    private readonly List<(Func<Asset, string, CancellationToken, bool> Match, Func<Asset, string, CancellationToken, Task<int>> Answer)> _updateAssetWithProductNameAsyncRules = [];

    /// <summary>Makes every later <c>UpdateAssetWithProductNameAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAssetWithProductNameAsync(Func<Asset, string, CancellationToken, Task<int>> answer) => AnswerUpdateAssetWithProductNameAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>UpdateAssetWithProductNameAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateAssetWithProductNameAsync(Func<Asset, string, CancellationToken, bool> match, Func<Asset, string, CancellationToken, Task<int>> answer)
    {
        lock (_updateAssetWithProductNameAsyncRules) _updateAssetWithProductNameAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName, CancellationToken ct)
    {
        lock (UpdateAssetWithProductNameAsyncCalls) UpdateAssetWithProductNameAsyncCalls.Add((asset, originalProductName, ct));
        Func<Asset, string, CancellationToken, Task<int>>? answer = null;
        lock (_updateAssetWithProductNameAsyncRules) answer = _updateAssetWithProductNameAsyncRules.LastOrDefault(rule => rule.Match(asset, originalProductName, ct)).Answer;
        if ((answer ?? OnUpdateAssetWithProductNameAsync) is { } on)
        {
            return on(asset, originalProductName, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateAssetWithProductNameAsync was called without an answer on a strict fake.") : Task.FromResult<int>(default);
    }

    /// <summary>What <c>LockReferencingRecordsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task>? OnLockReferencingRecordsAsync { get; set; }

    /// <summary>The arguments of every <c>LockReferencingRecordsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string ProductName, CancellationToken Ct)> LockReferencingRecordsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerLockReferencingRecordsAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task> Answer)> _lockReferencingRecordsAsyncRules = [];

    /// <summary>Makes every later <c>LockReferencingRecordsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerLockReferencingRecordsAsync(Func<string, string, CancellationToken, Task> answer) => AnswerLockReferencingRecordsAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>LockReferencingRecordsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerLockReferencingRecordsAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task> answer)
    {
        lock (_lockReferencingRecordsAsyncRules) _lockReferencingRecordsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task LockReferencingRecordsAsync(string accountEmail, string productName, CancellationToken ct)
    {
        lock (LockReferencingRecordsAsyncCalls) LockReferencingRecordsAsyncCalls.Add((accountEmail, productName, ct));
        Func<string, string, CancellationToken, Task>? answer = null;
        lock (_lockReferencingRecordsAsyncRules) answer = _lockReferencingRecordsAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productName, ct)).Answer;
        if ((answer ?? OnLockReferencingRecordsAsync) is { } on)
        {
            return on(accountEmail, productName, ct);
        }

        return Strict ? throw new InvalidOperationException("LockReferencingRecordsAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>GetByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<Asset>>>? OnGetByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>GetByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<Asset>>> Answer)> _getByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>GetByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, Task<List<Asset>>> answer) => AnswerGetByAccountEmailAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByAccountEmailAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<Asset>>> answer)
    {
        lock (_getByAccountEmailAsyncRules) _getByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Asset>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Add((accountEmail, ct));
        Func<string, CancellationToken, Task<List<Asset>>>? answer = null;
        lock (_getByAccountEmailAsyncRules) answer = _getByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, ct)).Answer;
        if ((answer ?? OnGetByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<Asset>>([]);
    }

    /// <summary>What <c>SearchByAccountEmailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<List<Asset>>>? OnSearchByAccountEmailAsync { get; set; }

    /// <summary>The arguments of every <c>SearchByAccountEmailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string Search, CancellationToken Ct)> SearchByAccountEmailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerSearchByAccountEmailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<List<Asset>>> Answer)> _searchByAccountEmailAsyncRules = [];

    /// <summary>Makes every later <c>SearchByAccountEmailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchByAccountEmailAsync(Func<string, string, CancellationToken, Task<List<Asset>>> answer) => AnswerSearchByAccountEmailAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>SearchByAccountEmailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSearchByAccountEmailAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<List<Asset>>> answer)
    {
        lock (_searchByAccountEmailAsyncRules) _searchByAccountEmailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Asset>> SearchByAccountEmailAsync(string accountEmail, string search, CancellationToken ct)
    {
        lock (SearchByAccountEmailAsyncCalls) SearchByAccountEmailAsyncCalls.Add((accountEmail, search, ct));
        Func<string, string, CancellationToken, Task<List<Asset>>>? answer = null;
        lock (_searchByAccountEmailAsyncRules) answer = _searchByAccountEmailAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, search, ct)).Answer;
        if ((answer ?? OnSearchByAccountEmailAsync) is { } on)
        {
            return on(accountEmail, search, ct);
        }

        return Strict ? throw new InvalidOperationException("SearchByAccountEmailAsync was called without an answer on a strict fake.") : Task.FromResult<List<Asset>>([]);
    }

    /// <summary>What <c>FindByEmailAndProductNameAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<Asset?>>? OnFindByEmailAndProductNameAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAndProductNameAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string ProductName, CancellationToken Ct)> FindByEmailAndProductNameAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAndProductNameAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<Asset?>> Answer)> _findByEmailAndProductNameAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAndProductNameAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNameAsync(Func<string, string, CancellationToken, Task<Asset?>> answer) => AnswerFindByEmailAndProductNameAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAndProductNameAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNameAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<Asset?>> answer)
    {
        lock (_findByEmailAndProductNameAsyncRules) _findByEmailAndProductNameAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Asset?> FindByEmailAndProductNameAsync(string accountEmail, string productName, CancellationToken ct)
    {
        lock (FindByEmailAndProductNameAsyncCalls) FindByEmailAndProductNameAsyncCalls.Add((accountEmail, productName, ct));
        Func<string, string, CancellationToken, Task<Asset?>>? answer = null;
        lock (_findByEmailAndProductNameAsyncRules) answer = _findByEmailAndProductNameAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productName, ct)).Answer;
        if ((answer ?? OnFindByEmailAndProductNameAsync) is { } on)
        {
            return on(accountEmail, productName, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAndProductNameAsync was called without an answer on a strict fake.") : Task.FromResult<Asset?>(default);
    }

    /// <summary>What <c>FindByEmailAndProductNameForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<Asset?>>? OnFindByEmailAndProductNameForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAndProductNameForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string ProductName, CancellationToken Ct)> FindByEmailAndProductNameForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAndProductNameForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<Asset?>> Answer)> _findByEmailAndProductNameForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAndProductNameForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNameForUpdateAsync(Func<string, string, CancellationToken, Task<Asset?>> answer) => AnswerFindByEmailAndProductNameForUpdateAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAndProductNameForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNameForUpdateAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<Asset?>> answer)
    {
        lock (_findByEmailAndProductNameForUpdateAsyncRules) _findByEmailAndProductNameForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Asset?> FindByEmailAndProductNameForUpdateAsync(string accountEmail, string productName, CancellationToken ct)
    {
        lock (FindByEmailAndProductNameForUpdateAsyncCalls) FindByEmailAndProductNameForUpdateAsyncCalls.Add((accountEmail, productName, ct));
        Func<string, string, CancellationToken, Task<Asset?>>? answer = null;
        lock (_findByEmailAndProductNameForUpdateAsyncRules) answer = _findByEmailAndProductNameForUpdateAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productName, ct)).Answer;
        if ((answer ?? OnFindByEmailAndProductNameForUpdateAsync) is { } on)
        {
            return on(accountEmail, productName, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAndProductNameForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<Asset?>(default);
    }

    /// <summary>What <c>FindByEmailAndProductNamesForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IReadOnlyCollection<string>, CancellationToken, Task<List<Asset>>>? OnFindByEmailAndProductNamesForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindByEmailAndProductNamesForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, IReadOnlyCollection<string> ProductNames, CancellationToken Ct)> FindByEmailAndProductNamesForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByEmailAndProductNamesForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, IReadOnlyCollection<string>, CancellationToken, bool> Match, Func<string, IReadOnlyCollection<string>, CancellationToken, Task<List<Asset>>> Answer)> _findByEmailAndProductNamesForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindByEmailAndProductNamesForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNamesForUpdateAsync(Func<string, IReadOnlyCollection<string>, CancellationToken, Task<List<Asset>>> answer) => AnswerFindByEmailAndProductNamesForUpdateAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>FindByEmailAndProductNamesForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByEmailAndProductNamesForUpdateAsync(Func<string, IReadOnlyCollection<string>, CancellationToken, bool> match, Func<string, IReadOnlyCollection<string>, CancellationToken, Task<List<Asset>>> answer)
    {
        lock (_findByEmailAndProductNamesForUpdateAsyncRules) _findByEmailAndProductNamesForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Asset>> FindByEmailAndProductNamesForUpdateAsync(string accountEmail, IReadOnlyCollection<string> productNames, CancellationToken ct)
    {
        lock (FindByEmailAndProductNamesForUpdateAsyncCalls) FindByEmailAndProductNamesForUpdateAsyncCalls.Add((accountEmail, productNames, ct));
        Func<string, IReadOnlyCollection<string>, CancellationToken, Task<List<Asset>>>? answer = null;
        lock (_findByEmailAndProductNamesForUpdateAsyncRules) answer = _findByEmailAndProductNamesForUpdateAsyncRules.LastOrDefault(rule => rule.Match(accountEmail, productNames, ct)).Answer;
        if ((answer ?? OnFindByEmailAndProductNamesForUpdateAsync) is { } on)
        {
            return on(accountEmail, productNames, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByEmailAndProductNamesForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<List<Asset>>([]);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Asset?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Asset? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<Asset?, CancellationToken, bool> Match, Func<Asset?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Asset?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Asset?, CancellationToken, bool> match, Func<Asset?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(Asset? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<Asset?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Asset?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Asset? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<Asset?, CancellationToken, bool> Match, Func<Asset?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Asset?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Asset?, CancellationToken, bool> match, Func<Asset?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(Asset? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<Asset?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => UpdateAssetWithProductNameAsyncCalls.Count + LockReferencingRecordsAsyncCalls.Count + GetByAccountEmailAsyncCalls.Count + SearchByAccountEmailAsyncCalls.Count + FindByEmailAndProductNameAsyncCalls.Count + FindByEmailAndProductNameForUpdateAsyncCalls.Count + FindByEmailAndProductNamesForUpdateAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (UpdateAssetWithProductNameAsyncCalls) UpdateAssetWithProductNameAsyncCalls.Clear();
        lock (LockReferencingRecordsAsyncCalls) LockReferencingRecordsAsyncCalls.Clear();
        lock (GetByAccountEmailAsyncCalls) GetByAccountEmailAsyncCalls.Clear();
        lock (SearchByAccountEmailAsyncCalls) SearchByAccountEmailAsyncCalls.Clear();
        lock (FindByEmailAndProductNameAsyncCalls) FindByEmailAndProductNameAsyncCalls.Clear();
        lock (FindByEmailAndProductNameForUpdateAsyncCalls) FindByEmailAndProductNameForUpdateAsyncCalls.Clear();
        lock (FindByEmailAndProductNamesForUpdateAsyncCalls) FindByEmailAndProductNamesForUpdateAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
