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
    private static readonly SendEmailMessage Message = new("to@test.com", "Subject", "Body", "corr-1");

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

    private static BasicDeliverEventArgs MakeDeliverArgs(byte[] body, ulong deliveryTag = 1, bool redelivered = false) =>
        new("consumer-tag", deliveryTag, redelivered, exchange: "", routingKey: QueueNames.Email,
            new BasicProperties(), body, CancellationToken.None);

    /// <summary>On message received async valid message invokes handler and acks.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_ValidMessage_InvokesHandlerAndAcks()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(Message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 7));

        handler.Verify(h => h.HandleAsync(
            It.Is<SendEmailMessage>(m => m == Message), It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(7, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicNackAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async invalid json on first delivery nacks with requeue (one retry) and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_FirstDelivery_NacksWithRequeueAndNeverCallsHandler()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: false));

        handler.Verify(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        channel.Verify(c => c.BasicNackAsync(3, false, true, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async invalid json on redelivery nacks without requeue and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_Redelivered_NacksWithoutRequeueAndNeverCallsHandler()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: true));

        handler.Verify(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        channel.Verify(c => c.BasicNackAsync(3, false, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async null json body on first delivery nacks with requeue (one retry).</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_FirstDelivery_NacksWithRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = "null"u8.ToArray();

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: false));

        channel.Verify(c => c.BasicNackAsync(4, false, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>On message received async null json body on redelivery nacks without requeue.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_Redelivered_NacksWithoutRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = "null"u8.ToArray();

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: true));

        channel.Verify(c => c.BasicNackAsync(4, false, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>On message received async handler throws on first delivery nacks with requeue (one retry), giving a transient SMTP blip a second chance.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_FirstDelivery_NacksWithRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(Message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: false));

        channel.Verify(c => c.BasicNackAsync(9, false, true, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>On message received async handler throws on redelivery nacks without requeue, so a genuinely poison message can't block the queue forever.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_Redelivered_NacksWithoutRequeue()
    {
        var handler = new Mock<IEmailMessageHandler>();
        handler.Setup(h => h.HandleAsync(It.IsAny<SendEmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));
        var worker = CreateSut(handler.Object, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(Message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: true));

        channel.Verify(c => c.BasicNackAsync(9, false, false, It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicAckAsync(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
