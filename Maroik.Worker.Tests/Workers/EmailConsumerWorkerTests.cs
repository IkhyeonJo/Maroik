using System.Text;
using System.Text.Json;
using Maroik.Core.Contract.Misc.Messaging;
using Maroik.Worker.Contracts;
using Maroik.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// Unit tests for <see cref="EmailConsumerWorker.OnMessageReceivedAsync"/> — the per-message
/// ack/nack decision that keeps a single poison message from blocking the "maroik.email" queue.
/// The channel is injected directly (via the internal <c>_channel</c> field, exposed to this
/// assembly through <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>)
/// so the connection/consume loop in <c>ExecuteAsync</c> doesn't need to run.
/// </summary>
public class EmailConsumerWorkerTests
{
    private static readonly SendEmailMessage _message = new("to@test.com", "Subject", "Body", "corr-1");

    private static IServiceScopeFactory BuildScopeFactory(IEmailMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static EmailConsumerWorker CreateSut(IEmailMessageHandler handler, out Mock<IChannel> channel)
    {
        var factory = new Mock<IConnectionFactory>();
        var worker = new EmailConsumerWorker(factory.Object, BuildScopeFactory(handler), Mock.Of<ILogger<EmailConsumerWorker>>());
        channel = new Mock<IChannel>();
        worker.Channel = channel.Object;
        return worker;
    }

    /// <param name="retryCount">
    /// Sets <see cref="EmailConsumerWorker.RetryCountHeader"/> on the delivery's properties —
    /// this worker's own retry counter, tracked in a republished header rather than via
    /// <c>BasicDeliverEventArgs.Redelivered</c> (see that constant's doc comment for why).
    /// </param>
    private static BasicDeliverEventArgs MakeDeliverArgs(byte[] body, ulong deliveryTag = 1, bool redelivered = false, int retryCount = 0)
    {
        var properties = new BasicProperties();
        if (retryCount > 0)
            properties.Headers = new Dictionary<string, object?> { [EmailConsumerWorker.RetryCountHeader] = retryCount };

        return new("consumer-tag", deliveryTag, redelivered, exchange: "", routingKey: QueueNames.Email,
            properties, body, CancellationToken.None);
    }

    /// <summary>On message received async valid message invokes handler and acks.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_ValidMessage_InvokesHandlerAndAcks()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 7));

        handler.Verify(h => h.HandleAsync(
            It.Is<SendEmailMessage>(m => m == _message), It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async invalid json on first delivery republishes for retry (ack, not nack) and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_FirstDelivery_RepublishesForRetryAndNeverCallsHandler()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");
        BasicProperties? published = null;
        channel.Setup(c => c.BasicPublishAsync(
                "", QueueNames.Email, false, It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>((_, _, _, props, _, _) => published = props)
            .Returns(ValueTask.CompletedTask);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: false));

        handler.Verify(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotNull(published);
        Assert.Equal(1, published!.Headers![EmailConsumerWorker.RetryCountHeader]);
        channel.Verify(c => c.BasicAckAsync(3, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async invalid json already retried once nacks without requeue and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_AlreadyRetried_NacksWithoutRequeueAndNeverCallsHandler()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        handler.Verify(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        channel.Verify(c => c.BasicNackAsync(3, false, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async null json body on first delivery republishes for retry.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_FirstDelivery_RepublishesForRetry()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = "null"u8.ToArray();
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: false));

        channel.Verify(c => c.BasicAckAsync(4, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async null json body already retried once nacks without requeue.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_AlreadyRetried_NacksWithoutRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = "null"u8.ToArray();

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        channel.Verify(c => c.BasicNackAsync(4, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>On message received async handler throws on first delivery republishes for retry, giving a transient SMTP blip a second chance.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_FirstDelivery_RepublishesForRetry()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: false));

        channel.Verify(c => c.BasicAckAsync(9, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async, when the handler succeeds but the ack itself throws, the message is neither nacked nor requeued — it must not be resent since the email already went out.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_AckThrowsAfterSuccessfulHandle_DoesNotNack()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        channel.Setup(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 11));

        handler.Verify(h => h.HandleAsync(
            It.Is<SendEmailMessage>(m => m == _message), It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression test: if the failure-path <c>BasicNackAsync</c> call itself throws (e.g. the
    /// channel is mid-recovery), that exception must be caught rather than escaping
    /// <c>OnMessageReceivedAsync</c> unhandled -- the success-path ack already has this protection,
    /// and the nack path needs the same so a broker blip during error handling doesn't surface as
    /// an unobserved/unhandled exception from the message loop.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NackThrowsAfterHandlerFailure_DoesNotThrow()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        channel.Setup(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        var exception = await Record.ExceptionAsync(() =>
            worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 13, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries)));

        Assert.Null(exception);
    }

    /// <summary>
    /// Regression test: if the retry-path <c>BasicPublishAsync</c> (or the ack that follows it)
    /// itself throws (e.g. the channel is mid-recovery), that exception must be caught rather than
    /// escaping <c>OnMessageReceivedAsync</c> unhandled.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_RepublishThrowsAfterHandlerFailure_DoesNotThrow()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        var exception = await Record.ExceptionAsync(() =>
            worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 14, redelivered: false)));

        Assert.Null(exception);
    }

    /// <summary>
    /// Regression test: a send still running when the shutdown drain deadline is reached must be
    /// cancelled (via the token every <c>HandleAsync</c> call is given), not left to race the
    /// channel disposal that follows <c>StopAsync</c>'s drain wait -- disposing the channel out
    /// from under a still-running send would throw from its own ack/nack and force a
    /// redelivery/resend of a send that may already have completed.
    /// </summary>
    [Fact]
    public async Task StopAsync_CancelsInFlightSend_WhenDrainDeadlineReached()
    {
        var handlerStarted = new TaskCompletionSource();
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .Returns(async (SendEmailMessage _, CancellationToken ct) =>
            {
                handlerStarted.SetResult();
                await Task.Delay(Timeout.Infinite, ct);
            });
        var worker = CreateSut(handler.Object, out var channel);
        worker.DrainTimeout = TimeSpan.FromMilliseconds(50);
        worker.CancelUnwindTimeout = TimeSpan.FromMilliseconds(500);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        Task received = worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 21, redelivered: false));
        await handlerStarted.Task;

        // StopAsync itself must return (the unwind wait isn't left hanging on the now-cancelled
        // send), and OnMessageReceivedAsync's own catch -- not an unhandled exception -- must be
        // what observes the cancellation: it never rethrows, it republishes for a first retry
        // (this is a first-attempt failure, so it isn't dead-lettered yet) and acks the original.
        await worker.StopAsync(CancellationToken.None);
        await received;

        channel.Verify(c => c.BasicAckAsync(21, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async handler throws after already being retried once nacks without requeue, so a genuinely poison message can't block the queue forever.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_AlreadyRetried_NacksWithoutRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        channel.Verify(c => c.BasicNackAsync(9, false, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression test: a message that the broker marks <c>Redelivered</c> for a reason unrelated
    /// to this worker's own retry decision (e.g. a connection drop right after this process started,
    /// before it ever attempted the message) must still get its full retry budget — the decision is
    /// keyed on <see cref="EmailConsumerWorker.RetryCountHeader"/>, not <c>Redelivered</c>.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_RedeliveredButNoRetryHeader_StillRepublishesForRetry()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(), It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // redelivered: true, but no retry header -- simulates a broker-level redelivery this
        // consumer never itself attempted (e.g. a fresh process after a crash).
        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 10, redelivered: true));

        channel.Verify(c => c.BasicAckAsync(10, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
