using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IBoardRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeBoardRepository : IBoardRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>IncrementViewAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task>? OnIncrementViewAsync { get; set; }

    /// <summary>The arguments of every <c>IncrementViewAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> IncrementViewAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerIncrementViewAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task> Answer)> _incrementViewAsyncRules = [];

    /// <summary>Makes every later <c>IncrementViewAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIncrementViewAsync(Func<long, CancellationToken, Task> answer) => AnswerIncrementViewAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>IncrementViewAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerIncrementViewAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task> answer)
    {
        lock (_incrementViewAsyncRules) _incrementViewAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task IncrementViewAsync(long id, CancellationToken ct)
    {
        lock (IncrementViewAsyncCalls) IncrementViewAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task>? answer = null;
        lock (_incrementViewAsyncRules) answer = _incrementViewAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnIncrementViewAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("IncrementViewAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>WriteBoardAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Board, CancellationToken, Task<long>>? OnWriteBoardAsync { get; set; }

    /// <summary>The arguments of every <c>WriteBoardAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Board Board, CancellationToken Ct)> WriteBoardAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerWriteBoardAsync</c> added, newest last.</summary>
    private readonly List<(Func<Board, CancellationToken, bool> Match, Func<Board, CancellationToken, Task<long>> Answer)> _writeBoardAsyncRules = [];

    /// <summary>Makes every later <c>WriteBoardAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerWriteBoardAsync(Func<Board, CancellationToken, Task<long>> answer) => AnswerWriteBoardAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>WriteBoardAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerWriteBoardAsync(Func<Board, CancellationToken, bool> match, Func<Board, CancellationToken, Task<long>> answer)
    {
        lock (_writeBoardAsyncRules) _writeBoardAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<long> WriteBoardAsync(Board board, CancellationToken ct)
    {
        lock (WriteBoardAsyncCalls) WriteBoardAsyncCalls.Add((board, ct));
        Func<Board, CancellationToken, Task<long>>? answer = null;
        lock (_writeBoardAsyncRules) answer = _writeBoardAsyncRules.LastOrDefault(rule => rule.Match(board, ct)).Answer;
        if ((answer ?? OnWriteBoardAsync) is { } on)
        {
            return on(board, ct);
        }

        return Strict ? throw new InvalidOperationException("WriteBoardAsync was called without an answer on a strict fake.") : Task.FromResult<long>(default);
    }

    /// <summary>What <c>GetNoticedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<List<Board>>>? OnGetNoticedAsync { get; set; }

    /// <summary>The arguments of every <c>GetNoticedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Type, CancellationToken Ct)> GetNoticedAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetNoticedAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, CancellationToken, bool> Match, Func<string, CancellationToken, Task<List<Board>>> Answer)> _getNoticedAsyncRules = [];

    /// <summary>Makes every later <c>GetNoticedAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetNoticedAsync(Func<string, CancellationToken, Task<List<Board>>> answer) => AnswerGetNoticedAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetNoticedAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetNoticedAsync(Func<string, CancellationToken, bool> match, Func<string, CancellationToken, Task<List<Board>>> answer)
    {
        lock (_getNoticedAsyncRules) _getNoticedAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<List<Board>> GetNoticedAsync(string type, CancellationToken ct)
    {
        lock (GetNoticedAsyncCalls) GetNoticedAsyncCalls.Add((type, ct));
        Func<string, CancellationToken, Task<List<Board>>>? answer = null;
        lock (_getNoticedAsyncRules) answer = _getNoticedAsyncRules.LastOrDefault(rule => rule.Match(type, ct)).Answer;
        if ((answer ?? OnGetNoticedAsync) is { } on)
        {
            return on(type, ct);
        }

        return Strict ? throw new InvalidOperationException("GetNoticedAsync was called without an answer on a strict fake.") : Task.FromResult<List<Board>>([]);
    }

    /// <summary>What <c>QueryPageAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<BoardPageQuery, CancellationToken, Task<(List<Board>, int)>>? OnQueryPageAsync { get; set; }

    /// <summary>The arguments of every <c>QueryPageAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(BoardPageQuery Query, CancellationToken Ct)> QueryPageAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerQueryPageAsync</c> added, newest last.</summary>
    private readonly List<(Func<BoardPageQuery, CancellationToken, bool> Match, Func<BoardPageQuery, CancellationToken, Task<(List<Board>, int)>> Answer)> _queryPageAsyncRules = [];

    /// <summary>Makes every later <c>QueryPageAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerQueryPageAsync(Func<BoardPageQuery, CancellationToken, Task<(List<Board>, int)>> answer) => AnswerQueryPageAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>QueryPageAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerQueryPageAsync(Func<BoardPageQuery, CancellationToken, bool> match, Func<BoardPageQuery, CancellationToken, Task<(List<Board>, int)>> answer)
    {
        lock (_queryPageAsyncRules) _queryPageAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<(List<Board>, int)> QueryPageAsync(BoardPageQuery query, CancellationToken ct)
    {
        lock (QueryPageAsyncCalls) QueryPageAsyncCalls.Add((query, ct));
        Func<BoardPageQuery, CancellationToken, Task<(List<Board>, int)>>? answer = null;
        lock (_queryPageAsyncRules) answer = _queryPageAsyncRules.LastOrDefault(rule => rule.Match(query, ct)).Answer;
        if ((answer ?? OnQueryPageAsync) is { } on)
        {
            return on(query, ct);
        }

        return Strict ? throw new InvalidOperationException("QueryPageAsync was called without an answer on a strict fake.") : Task.FromResult<(List<Board>, int)>(default);
    }

    /// <summary>What <c>FindActiveByIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Board?>>? OnFindActiveByIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindActiveByIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindActiveByIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindActiveByIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Board?>> Answer)> _findActiveByIdAsyncRules = [];

    /// <summary>Makes every later <c>FindActiveByIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveByIdAsync(Func<long, CancellationToken, Task<Board?>> answer) => AnswerFindActiveByIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindActiveByIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveByIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Board?>> answer)
    {
        lock (_findActiveByIdAsyncRules) _findActiveByIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Board?> FindActiveByIdAsync(long id, CancellationToken ct)
    {
        lock (FindActiveByIdAsyncCalls) FindActiveByIdAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Board?>>? answer = null;
        lock (_findActiveByIdAsyncRules) answer = _findActiveByIdAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindActiveByIdAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindActiveByIdAsync was called without an answer on a strict fake.") : Task.FromResult<Board?>(default);
    }

    /// <summary>What <c>FindActiveByIdForUpdateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<Board?>>? OnFindActiveByIdForUpdateAsync { get; set; }

    /// <summary>The arguments of every <c>FindActiveByIdForUpdateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long Id, CancellationToken Ct)> FindActiveByIdForUpdateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindActiveByIdForUpdateAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<Board?>> Answer)> _findActiveByIdForUpdateAsyncRules = [];

    /// <summary>Makes every later <c>FindActiveByIdForUpdateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveByIdForUpdateAsync(Func<long, CancellationToken, Task<Board?>> answer) => AnswerFindActiveByIdForUpdateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindActiveByIdForUpdateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindActiveByIdForUpdateAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<Board?>> answer)
    {
        lock (_findActiveByIdForUpdateAsyncRules) _findActiveByIdForUpdateAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Board?> FindActiveByIdForUpdateAsync(long id, CancellationToken ct)
    {
        lock (FindActiveByIdForUpdateAsyncCalls) FindActiveByIdForUpdateAsyncCalls.Add((id, ct));
        Func<long, CancellationToken, Task<Board?>>? answer = null;
        lock (_findActiveByIdForUpdateAsyncRules) answer = _findActiveByIdForUpdateAsyncRules.LastOrDefault(rule => rule.Match(id, ct)).Answer;
        if ((answer ?? OnFindActiveByIdForUpdateAsync) is { } on)
        {
            return on(id, ct);
        }

        return Strict ? throw new InvalidOperationException("FindActiveByIdForUpdateAsync was called without an answer on a strict fake.") : Task.FromResult<Board?>(default);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Board?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Board? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<Board?, CancellationToken, bool> Match, Func<Board?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Board?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<Board?, CancellationToken, bool> match, Func<Board?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(Board? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<Board?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Board?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Board? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<Board?, CancellationToken, bool> Match, Func<Board?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Board?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<Board?, CancellationToken, bool> match, Func<Board?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(Board? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<Board?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => IncrementViewAsyncCalls.Count + WriteBoardAsyncCalls.Count + GetNoticedAsyncCalls.Count + QueryPageAsyncCalls.Count + FindActiveByIdAsyncCalls.Count + FindActiveByIdForUpdateAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (IncrementViewAsyncCalls) IncrementViewAsyncCalls.Clear();
        lock (WriteBoardAsyncCalls) WriteBoardAsyncCalls.Clear();
        lock (GetNoticedAsyncCalls) GetNoticedAsyncCalls.Clear();
        lock (QueryPageAsyncCalls) QueryPageAsyncCalls.Clear();
        lock (FindActiveByIdAsyncCalls) FindActiveByIdAsyncCalls.Clear();
        lock (FindActiveByIdForUpdateAsyncCalls) FindActiveByIdForUpdateAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
