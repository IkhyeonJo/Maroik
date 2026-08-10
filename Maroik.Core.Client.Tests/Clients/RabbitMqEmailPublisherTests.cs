using Maroik.Core.Client.Clients;
using Maroik.Core.Contract.Misc.Messaging;
using Moq;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="RabbitMqEmailPublisher"/>.
/// Verifies lazy connection creation, connection reuse while open, reconnection once the
/// cached connection drops, and the shape of the published AMQP message.
/// </summary>
public class RabbitMqEmailPublisherTests
{
    private readonly Mock<IConnectionFactory> _factory = new();
    private static readonly SendEmailMessage Message = new("to@test.com", "Subject", "Body", "corr-1");

    private static Mock<IChannel> MakeChannelMock()
    {
        var channel = new Mock<IChannel>();
        channel.Setup(c => c.QueueDeclareAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<IDictionary<string, object?>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QueueDeclareOk)null!);
        return channel;
    }

    private static Mock<IConnection> MakeConnectionMock(Mock<IChannel> channel, bool isOpen) =>
        MakeConnectionMock(channel.Object, isOpen);

    private static Mock<IConnection> MakeConnectionMock(IChannel channel, bool isOpen)
    {
        var connection = new Mock<IConnection>();
        connection.Setup(c => c.IsOpen).Returns(isOpen);
        connection.Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel);
        return connection;
    }

    private RabbitMqEmailPublisher CreateSut() => new(_factory.Object);

    /// <summary>Publish async no cached connection opens connection and publishes.</summary>
    [Fact]
    public async Task PublishAsync_NoCachedConnection_OpensConnectionAndPublishes()
    {
        var channel = MakeChannelMock();
        var connection = MakeConnectionMock(channel, isOpen: true);
        _factory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);

        await CreateSut().PublishAsync(Message, TestContext.Current.CancellationToken);

        _factory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicPublishAsync(
                "",
                QueueNames.Email,
                false,
                It.Is<BasicProperties>(p => p.Persistent == true),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Publish async publishes message body as json.</summary>
    [Fact]
    public async Task PublishAsync_PublishesMessageBodyAsJson()
    {
        var channel = MakeChannelMock();
        var connection = MakeConnectionMock(channel, isOpen: true);
        _factory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        byte[]? capturedBody = null;
        channel.Setup(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, _, _, _, body, _) => capturedBody = body.ToArray());

        await CreateSut().PublishAsync(Message, TestContext.Current.CancellationToken);

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailMessage>(capturedBody!);
        Assert.Equal(Message, deserialized);
    }

    /// <summary>Publish async connection stays open reuses connection across calls.</summary>
    [Fact]
    public async Task PublishAsync_ConnectionStaysOpen_ReusesConnectionAcrossCalls()
    {
        var channel = MakeChannelMock();
        var connection = MakeConnectionMock(channel, isOpen: true);
        _factory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        var sut = CreateSut();

        await sut.PublishAsync(Message, TestContext.Current.CancellationToken);
        await sut.PublishAsync(Message, TestContext.Current.CancellationToken);

        _factory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        channel.Verify(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    /// <summary>Publish async cached connection closed disposes it and opens a new one.</summary>
    [Fact]
    public async Task PublishAsync_CachedConnectionClosed_DisposesItAndOpensANewOne()
    {
        var channel1 = MakeChannelMock();
        var deadConnection = MakeConnectionMock(channel1, isOpen: false);
        var channel2 = MakeChannelMock();
        var freshConnection = MakeConnectionMock(channel2, isOpen: true);
        _factory.SetupSequence(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadConnection.Object)
            .ReturnsAsync(freshConnection.Object);
        var sut = CreateSut();

        await sut.PublishAsync(Message, TestContext.Current.CancellationToken); // caches deadConnection (its own open state is irrelevant on creation)
        await sut.PublishAsync(Message, TestContext.Current.CancellationToken); // now sees deadConnection.IsOpen == false, must reconnect

        _factory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        deadConnection.Verify(c => c.DisposeAsync(), Times.Once);
        channel2.Verify(c => c.BasicPublishAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>Dispose async with open connection disposes connection.</summary>
    [Fact]
    public async Task DisposeAsync_WithOpenConnection_DisposesConnection()
    {
        var channel = MakeChannelMock();
        var connection = MakeConnectionMock(channel, isOpen: true);
        _factory.Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(connection.Object);
        var sut = CreateSut();
        await sut.PublishAsync(Message, TestContext.Current.CancellationToken);

        await sut.DisposeAsync();

        connection.Verify(c => c.DisposeAsync(), Times.Once);
    }

    /// <summary>Dispose async never published does not throw.</summary>
    [Fact]
    public async Task DisposeAsync_NeverPublished_DoesNotThrow()
    {
        var sut = CreateSut();

        await sut.DisposeAsync();

        _factory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
