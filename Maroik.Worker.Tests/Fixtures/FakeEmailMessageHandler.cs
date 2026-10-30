using Maroik.Core.Contract.Messages;
using Maroik.Worker.Interfaces;

namespace Maroik.Worker.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IEmailMessageHandler"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeEmailMessageHandler : IEmailMessageHandler
{
    /// <summary>What <c>HandleAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<SendEmailMessage, CancellationToken, Task>? OnHandleAsync { get; set; }

    /// <summary>The arguments of every <c>HandleAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(SendEmailMessage Message, CancellationToken Ct)> HandleAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task HandleAsync(SendEmailMessage message, CancellationToken ct)
    {
        lock (HandleAsyncCalls) HandleAsyncCalls.Add((message, ct));
        if (OnHandleAsync is { } on)
        {
            return on(message, ct);
        }

        return Task.CompletedTask;
    }
}
