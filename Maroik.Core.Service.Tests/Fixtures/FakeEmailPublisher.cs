using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IEmailPublisher"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeEmailPublisher : IEmailPublisher
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>PublishAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<SendEmailMessage, CancellationToken, Task>? OnPublishAsync { get; set; }

    /// <summary>The arguments of every <c>PublishAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(SendEmailMessage Message, CancellationToken Ct)> PublishAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerPublishAsync</c> added, newest last.</summary>
    private readonly List<(Func<SendEmailMessage, CancellationToken, bool> Match, Func<SendEmailMessage, CancellationToken, Task> Answer)> _publishAsyncRules = [];

    /// <summary>Makes every later <c>PublishAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerPublishAsync(Func<SendEmailMessage, CancellationToken, Task> answer) => AnswerPublishAsync((_, _) => true, answer);

    /// <summary>Makes a later <c>PublishAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerPublishAsync(Func<SendEmailMessage, CancellationToken, bool> match, Func<SendEmailMessage, CancellationToken, Task> answer)
    {
        lock (_publishAsyncRules) _publishAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task PublishAsync(SendEmailMessage message, CancellationToken ct)
    {
        lock (PublishAsyncCalls) PublishAsyncCalls.Add((message, ct));
        Func<SendEmailMessage, CancellationToken, Task>? answer = null;
        lock (_publishAsyncRules) answer = _publishAsyncRules.LastOrDefault(rule => rule.Match(message, ct)).Answer;
        if ((answer ?? OnPublishAsync) is { } on)
        {
            return on(message, ct);
        }

        return Strict ? throw new InvalidOperationException("PublishAsync was called without an answer on a strict fake.") : Task.CompletedTask;
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => PublishAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (PublishAsyncCalls) PublishAsyncCalls.Clear();
    }
}
