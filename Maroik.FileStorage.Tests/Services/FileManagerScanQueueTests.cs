using Maroik.FileStorage.Services;

namespace Maroik.FileStorage.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FileManagerScanQueue"/>, the in-memory list of the File Manager scans waiting or running: a queued upload is
/// handed out once and stays pending until it is finished, a refusal is answered once, and an abort cancels the scan and forgets it.
/// </summary>
public sealed class FileManagerScanQueueTests
{
    /// <summary>The upload id these tests use.</summary>
    private const string UploadId = "0f8fad5b-d9cb-469f-a165-70867728950e";

    /// <summary>A queued upload is handed out by the next dequeue and stays pending until it is finished.</summary>
    [Fact]
    public async Task AQueuedUpload_IsHandedOut_AndStaysPendingUntilFinished()
    {
        var queue = new FileManagerScanQueue();

        Assert.False(queue.IsPending(UploadId));
        Assert.True(queue.TryEnqueue(UploadId));
        Assert.True(queue.IsPending(UploadId));

        (string id, CancellationToken aborted) = await queue.DequeueAsync(TestContext.Current.CancellationToken);
        Assert.Equal(UploadId, id);
        Assert.False(aborted.IsCancellationRequested);
        Assert.True(queue.IsPending(UploadId));

        queue.Finish(UploadId, null);
        Assert.False(queue.IsPending(UploadId));
        Assert.Null(queue.TakeRefusal(UploadId));
    }

    /// <summary>An upload already pending is not queued twice (a second complete while it is scanning).</summary>
    [Fact]
    public async Task TryEnqueue_RefusesAnUploadAlreadyPending()
    {
        var queue = new FileManagerScanQueue();

        Assert.True(queue.TryEnqueue(UploadId));
        Assert.False(queue.TryEnqueue(UploadId));

        await queue.DequeueAsync(TestContext.Current.CancellationToken);
        using var nothingElse = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        nothingElse.CancelAfter(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await queue.DequeueAsync(nothingElse.Token));
    }

    /// <summary>A refusal a scan finished with is answered once; then the upload is unknown again.</summary>
    [Fact]
    public async Task ARefusal_IsAnsweredOnce()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);

        queue.Finish(UploadId, "infected");

        Assert.False(queue.IsPending(UploadId));
        Assert.Equal("infected", queue.TakeRefusal(UploadId));
        Assert.Null(queue.TakeRefusal(UploadId));
    }

    /// <summary>Cancelling a pending upload cancels the token its scan runs with, and the refusal it then finishes with is not kept.</summary>
    [Fact]
    public async Task Cancel_CancelsTheScan_AndItsRefusalIsNotKept()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        (_, CancellationToken aborted) = await queue.DequeueAsync(TestContext.Current.CancellationToken);

        queue.Cancel(UploadId);
        Assert.True(aborted.IsCancellationRequested);
        queue.Finish(UploadId, "could not be scanned");

        Assert.False(queue.IsPending(UploadId));
        Assert.Null(queue.TakeRefusal(UploadId));
    }

    /// <summary>Cancelling forgets a refusal that was already kept; cancelling an unknown upload does nothing.</summary>
    [Fact]
    public async Task Cancel_ForgetsAKeptRefusal()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);
        queue.Finish(UploadId, "infected");

        queue.Cancel(UploadId);
        queue.Cancel("1f8fad5b-d9cb-469f-a165-70867728950e");

        Assert.Null(queue.TakeRefusal(UploadId));
    }

    /// <summary>
    /// A refusal that leaves the file waiting for its owner's decision (the scan could not be finished) is answered on every ask until the
    /// decision takes it; then the upload is unknown again.
    /// </summary>
    [Fact]
    public async Task AnUndecidedRefusal_IsAnsweredUntilTheDecisionTakesIt()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);

        queue.Finish(UploadId, "too large", awaitsDecision: true);

        Assert.Equal("too large", queue.TakeRefusal(UploadId));
        Assert.Equal("too large", queue.TakeRefusal(UploadId));
        Assert.True(queue.TakeUndecided(UploadId));
        Assert.Null(queue.TakeRefusal(UploadId));
        Assert.False(queue.TakeUndecided(UploadId));
    }

    /// <summary>A plain refusal (an infected file) awaits no decision: the decision finds nothing to take and leaves the refusal in place.</summary>
    [Fact]
    public async Task TakeUndecided_IsFalse_ForARefusalThatAwaitsNoDecision()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);
        queue.Finish(UploadId, "infected");

        Assert.False(queue.TakeUndecided(UploadId));
        Assert.Equal("infected", queue.TakeRefusal(UploadId));
    }

    /// <summary>Cancelling (the owner said no, or the upload was abandoned) forgets an undecided refusal.</summary>
    [Fact]
    public async Task Cancel_ForgetsAnUndecidedRefusal()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);
        queue.Finish(UploadId, "too large", awaitsDecision: true);

        queue.Cancel(UploadId);

        Assert.Null(queue.TakeRefusal(UploadId));
        Assert.False(queue.TakeUndecided(UploadId));
    }

    /// <summary>An aborted scan that finishes "undecided" keeps nothing either.</summary>
    [Fact]
    public async Task AnAbortedScan_KeepsNoUndecidedRefusal()
    {
        var queue = new FileManagerScanQueue();
        queue.TryEnqueue(UploadId);
        await queue.DequeueAsync(TestContext.Current.CancellationToken);

        queue.Cancel(UploadId);
        queue.Finish(UploadId, "could not be scanned", awaitsDecision: true);

        Assert.Null(queue.TakeRefusal(UploadId));
        Assert.False(queue.TakeUndecided(UploadId));
    }
}
