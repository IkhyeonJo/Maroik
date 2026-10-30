using Maroik.Core.Contract.Interfaces;

namespace Maroik.Worker.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IAccountMailStatusService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeAccountMailStatusService : IAccountMailStatusService
{
    /// <summary>What <c>MarkMailSendFailedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnMarkMailSendFailedAsync { get; set; }

    /// <summary>The arguments of every <c>MarkMailSendFailedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> MarkMailSendFailedAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task MarkMailSendFailedAsync(string email, CancellationToken ct)
    {
        lock (MarkMailSendFailedAsyncCalls) MarkMailSendFailedAsyncCalls.Add((email, ct));
        if (OnMarkMailSendFailedAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ClearMailSendFailedAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnClearMailSendFailedAsync { get; set; }

    /// <summary>The arguments of every <c>ClearMailSendFailedAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, CancellationToken Ct)> ClearMailSendFailedAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ClearMailSendFailedAsync(string email, CancellationToken ct)
    {
        lock (ClearMailSendFailedAsyncCalls) ClearMailSendFailedAsyncCalls.Add((email, ct));
        if (OnClearMailSendFailedAsync is { } on)
        {
            return on(email, ct);
        }

        return Task.CompletedTask;
    }
}
