namespace Maroik.Core.Client.Tests.Fixtures;

/// <summary>
/// The fake SMTP server shuts down cleanly however its accept loop is caught by the shutdown. <c>DisposeAsync</c> signals the stop
/// and closes the listener while the loop may be anywhere: not yet at to accept (it then meets a closed listener —
/// "Not listening"), or inside it (the pending accept ends canceled, or — when closing the socket wins the race with the token,
/// as seen on CI — as a <c>SocketException</c> "Operation canceled"). Each of those is the shutdown, not a failure of the test
/// that used the server (<c>MailClientTests.SendMailAsync_ReturnsAGenericFailure_WhenTheServerIsUnreachable</c> failed on CI that
/// way, 2026-11-02).
/// </summary>
public class FakeSmtpServerTests
{
    /// <summary>
    /// A server disposed right after it started catches its loop at every point of the race; enough of them hit each one. No
    /// dispose may end in an exception.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_RightAfterStart_EndsCleanly_WhereverTheAcceptLoopIs()
    {
        var failures = new List<string>();

        for (int i = 0; i < 2000; i++)
        {
            if (await StartAndDisposeAsync() is { } failure) failures.Add(failure);
        }

        Assert.Empty(failures);
    }

    /// <summary>Starts a server and disposes it at once; the exception the dispose ended in, or null.</summary>
    private static async Task<string?> StartAndDisposeAsync()
    {
        try
        {
            await using var server = new FakeSmtpServer();
            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }
}
