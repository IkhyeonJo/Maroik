using Maroik.Core.Contract.Interfaces;
using System.Data;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IUnitOfWork"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>BeginAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IsolationLevel?, CancellationToken, Task>? OnBeginAsync { get; set; }

    /// <summary>The arguments of every <c>BeginAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IsolationLevel? IsolationLevel, CancellationToken Ct)> BeginAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerBeginAsync</c> added, newest last.</summary>
    private readonly List<(Func<IsolationLevel?, CancellationToken, bool> Match, Func<IsolationLevel?, CancellationToken, Task> Answer)> _beginAsyncRules = [];

    /// <summary>Makes every later <c>BeginAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerBeginAsync(Func<IsolationLevel?, CancellationToken, Task> answer) => AnswerBeginAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>BeginAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerBeginAsync(Func<IsolationLevel?, CancellationToken, bool> match, Func<IsolationLevel?, CancellationToken, Task> answer)
    {
        lock (_beginAsyncRules) _beginAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task BeginAsync(IsolationLevel? isolationLevel, CancellationToken ct)
    {
        lock (BeginAsyncCalls) BeginAsyncCalls.Add((isolationLevel, ct));
        Func<IsolationLevel?, CancellationToken, Task>? answer = null;
        lock (_beginAsyncRules) answer = _beginAsyncRules.LastOrDefault(rule => rule.Match(isolationLevel, ct)).Answer;
        if ((answer ?? OnBeginAsync) is { } on)
        {
            return on(isolationLevel, ct);
        }

        return Strict ? throw new InvalidOperationException("BeginAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>CommitAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task>? OnCommitAsync { get; set; }

    /// <summary>The arguments of every <c>CommitAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> CommitAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCommitAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task> Answer)> _commitAsyncRules = [];

    /// <summary>Makes every later <c>CommitAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCommitAsync(Func<CancellationToken, Task> answer) => AnswerCommitAsync((_) => true, answer);

    /// <summary>Makes a later <c>CommitAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCommitAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task> answer)
    {
        lock (_commitAsyncRules) _commitAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task CommitAsync(CancellationToken ct)
    {
        lock (CommitAsyncCalls) CommitAsyncCalls.Add(ct);
        Func<CancellationToken, Task>? answer = null;
        lock (_commitAsyncRules) answer = _commitAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnCommitAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("CommitAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>RollbackAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task>? OnRollbackAsync { get; set; }

    /// <summary>The arguments of every <c>RollbackAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CancellationToken> RollbackAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerRollbackAsync</c> added, newest last.</summary>
    private readonly List<(Func<CancellationToken, bool> Match, Func<CancellationToken, Task> Answer)> _rollbackAsyncRules = [];

    /// <summary>Makes every later <c>RollbackAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerRollbackAsync(Func<CancellationToken, Task> answer) => AnswerRollbackAsync((_) => true, answer);

    /// <summary>Makes a later <c>RollbackAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerRollbackAsync(Func<CancellationToken, bool> match, Func<CancellationToken, Task> answer)
    {
        lock (_rollbackAsyncRules) _rollbackAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task RollbackAsync(CancellationToken ct)
    {
        lock (RollbackAsyncCalls) RollbackAsyncCalls.Add(ct);
        Func<CancellationToken, Task>? answer = null;
        lock (_rollbackAsyncRules) answer = _rollbackAsyncRules.LastOrDefault(rule => rule.Match(ct)).Answer;
        if ((answer ?? OnRollbackAsync) is { } on)
        {
            return on(ct);
        }

        return Strict ? throw new InvalidOperationException("RollbackAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>What <c>DisposeAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ValueTask>? OnDisposeAsync { get; set; }

    /// <summary>How many times <c>DisposeAsync</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int DisposeAsyncCallCount => Volatile.Read(ref _disposeAsyncCallCount);

    /// <summary>Backs <see cref="DisposeAsyncCallCount"/>.</summary>
    private int _disposeAsyncCallCount;

    /// <summary>The rules <c>AnswerDisposeAsync</c> added, newest last.</summary>
    private readonly List<(Func<bool> Match, Func<ValueTask> Answer)> _disposeAsyncRules = [];

    /// <summary>Makes every later <c>DisposeAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDisposeAsync(Func<ValueTask> answer) => AnswerDisposeAsync(() => true, answer);

    /// <summary>Makes a later <c>DisposeAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDisposeAsync(Func<bool> match, Func<ValueTask> answer)
    {
        lock (_disposeAsyncRules) _disposeAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeAsyncCallCount);
        Func<ValueTask>? answer = null;
        lock (_disposeAsyncRules) answer = _disposeAsyncRules.LastOrDefault(rule => rule.Match()).Answer;
        if ((answer ?? OnDisposeAsync) is { } on)
        {
            return on();
        }

        return Strict ? throw new InvalidOperationException("DisposeAsync was called without an answer on a strict fake.") : ValueTask.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => BeginAsyncCalls.Count + CommitAsyncCalls.Count + RollbackAsyncCalls.Count + DisposeAsyncCallCount;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (BeginAsyncCalls) BeginAsyncCalls.Clear();
        lock (CommitAsyncCalls) CommitAsyncCalls.Clear();
        lock (RollbackAsyncCalls) RollbackAsyncCalls.Clear();
        Interlocked.Exchange(ref _disposeAsyncCallCount, 0);
    }
}
