using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IBoardAttachedFileRepository"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeBoardAttachedFileRepository : IBoardAttachedFileRepository
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetByBoardIdsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, BoardAttachedFile?>>>? OnGetByBoardIdsAsync { get; set; }

    /// <summary>The arguments of every <c>GetByBoardIdsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IEnumerable<long> BoardIds, CancellationToken Ct)> GetByBoardIdsAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetByBoardIdsAsync</c> added, newest last.</summary>
    private readonly List<(Func<IEnumerable<long>, CancellationToken, bool> Match, Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, BoardAttachedFile?>>> Answer)> _getByBoardIdsAsyncRules = [];

    /// <summary>Makes every later <c>GetByBoardIdsAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByBoardIdsAsync(Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, BoardAttachedFile?>>> answer) => AnswerGetByBoardIdsAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>GetByBoardIdsAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetByBoardIdsAsync(Func<IEnumerable<long>, CancellationToken, bool> match, Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, BoardAttachedFile?>>> answer)
    {
        lock (_getByBoardIdsAsyncRules) _getByBoardIdsAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct)
    {
        lock (GetByBoardIdsAsyncCalls) GetByBoardIdsAsyncCalls.Add((boardIds, ct));
        Func<IEnumerable<long>, CancellationToken, Task<Dictionary<long, BoardAttachedFile?>>>? answer = null;
        lock (_getByBoardIdsAsyncRules) answer = _getByBoardIdsAsyncRules.LastOrDefault(rule => rule.Match(boardIds, ct)).Answer;
        if ((answer ?? OnGetByBoardIdsAsync) is { } on)
        {
            return on(boardIds, ct);
        }

        return Strict ? throw new InvalidOperationException("GetByBoardIdsAsync was called without an answer on a strict fake.") : Task.FromResult<Dictionary<long, BoardAttachedFile?>>([]);
    }

    /// <summary>What <c>FindByBoardIdAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<long, CancellationToken, Task<BoardAttachedFile?>>? OnFindByBoardIdAsync { get; set; }

    /// <summary>The arguments of every <c>FindByBoardIdAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(long BoardId, CancellationToken Ct)> FindByBoardIdAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerFindByBoardIdAsync</c> added, newest last.</summary>
    private readonly List<(Func<long, CancellationToken, bool> Match, Func<long, CancellationToken, Task<BoardAttachedFile?>> Answer)> _findByBoardIdAsyncRules = [];

    /// <summary>Makes every later <c>FindByBoardIdAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByBoardIdAsync(Func<long, CancellationToken, Task<BoardAttachedFile?>> answer) => AnswerFindByBoardIdAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>FindByBoardIdAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerFindByBoardIdAsync(Func<long, CancellationToken, bool> match, Func<long, CancellationToken, Task<BoardAttachedFile?>> answer)
    {
        lock (_findByBoardIdAsyncRules) _findByBoardIdAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<BoardAttachedFile?> FindByBoardIdAsync(long boardId, CancellationToken ct)
    {
        lock (FindByBoardIdAsyncCalls) FindByBoardIdAsyncCalls.Add((boardId, ct));
        Func<long, CancellationToken, Task<BoardAttachedFile?>>? answer = null;
        lock (_findByBoardIdAsyncRules) answer = _findByBoardIdAsyncRules.LastOrDefault(rule => rule.Match(boardId, ct)).Answer;
        if ((answer ?? OnFindByBoardIdAsync) is { } on)
        {
            return on(boardId, ct);
        }

        return Strict ? throw new InvalidOperationException("FindByBoardIdAsync was called without an answer on a strict fake.") : Task.FromResult<BoardAttachedFile?>(default);
    }

    /// <summary>What <c>CreateAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<BoardAttachedFile?, CancellationToken, Task>? OnCreateAsync { get; set; }

    /// <summary>The arguments of every <c>CreateAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(BoardAttachedFile? Domain, CancellationToken Ct)> CreateAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCreateAsync</c> added, newest last.</summary>
    private readonly List<(Func<BoardAttachedFile?, CancellationToken, bool> Match, Func<BoardAttachedFile?, CancellationToken, Task> Answer)> _createAsyncRules = [];

    /// <summary>Makes every later <c>CreateAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<BoardAttachedFile?, CancellationToken, Task> answer) => AnswerCreateAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>CreateAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCreateAsync(Func<BoardAttachedFile?, CancellationToken, bool> match, Func<BoardAttachedFile?, CancellationToken, Task> answer)
    {
        lock (_createAsyncRules) _createAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CreateAsync(BoardAttachedFile? domain, CancellationToken ct)
    {
        lock (CreateAsyncCalls) CreateAsyncCalls.Add((domain, ct));
        Func<BoardAttachedFile?, CancellationToken, Task>? answer = null;
        lock (_createAsyncRules) answer = _createAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnCreateAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("CreateAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>UpdateEntityAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<BoardAttachedFile?, CancellationToken, Task>? OnUpdateEntityAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateEntityAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(BoardAttachedFile? Domain, CancellationToken Ct)> UpdateEntityAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUpdateEntityAsync</c> added, newest last.</summary>
    private readonly List<(Func<BoardAttachedFile?, CancellationToken, bool> Match, Func<BoardAttachedFile?, CancellationToken, Task> Answer)> _updateEntityAsyncRules = [];

    /// <summary>Makes every later <c>UpdateEntityAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<BoardAttachedFile?, CancellationToken, Task> answer) => AnswerUpdateEntityAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>UpdateEntityAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUpdateEntityAsync(Func<BoardAttachedFile?, CancellationToken, bool> match, Func<BoardAttachedFile?, CancellationToken, Task> answer)
    {
        lock (_updateEntityAsyncRules) _updateEntityAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task UpdateEntityAsync(BoardAttachedFile? domain, CancellationToken ct)
    {
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Add((domain, ct));
        Func<BoardAttachedFile?, CancellationToken, Task>? answer = null;
        lock (_updateEntityAsyncRules) answer = _updateEntityAsyncRules.LastOrDefault(rule => rule.Match(domain, ct)).Answer;
        if ((answer ?? OnUpdateEntityAsync) is { } on)
        {
            return on(domain, ct);
        }

        return Strict ? throw new InvalidOperationException("UpdateEntityAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetByBoardIdsAsyncCalls.Count + FindByBoardIdAsyncCalls.Count + CreateAsyncCalls.Count + UpdateEntityAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetByBoardIdsAsyncCalls) GetByBoardIdsAsyncCalls.Clear();
        lock (FindByBoardIdAsyncCalls) FindByBoardIdAsyncCalls.Clear();
        lock (CreateAsyncCalls) CreateAsyncCalls.Clear();
        lock (UpdateEntityAsyncCalls) UpdateEntityAsyncCalls.Clear();
    }
}
