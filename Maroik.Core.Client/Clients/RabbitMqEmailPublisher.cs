using System.Text.Json;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Messaging;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Publishes <see cref="SendEmailMessage"/> to the durable "maroik.email" queue.
/// The connection and a publish channel are opened lazily on first publish (so a RabbitMQ outage
/// at application startup does not prevent the web app from starting) and then cached and reused
/// across publishes; the queue is declared once per channel rather than on every call.
/// </summary>
public sealed class RabbitMqEmailPublisher : IEmailPublisher, IAsyncDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>Initializes a new instance of <see cref="RabbitMqEmailPublisher"/>. Does not open a connection yet.</summary>
    public RabbitMqEmailPublisher(IConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task PublishAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        IChannel channel = await GetChannelAsync(ct);

        byte[] body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: QueueNames.Email,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: ct);
    }

    /// <summary>
    /// Returns a ready-to-publish channel, opening the connection and channel (and declaring the
    /// queue once) on first use or after either has dropped. Double-checked locking keeps the
    /// common path — connection and channel already open — lock-free. Publisher-confirmation
    /// tracking serialises concurrent publishes on the cached channel internally, and the client's
    /// automatic / topology recovery re-opens and re-declares transparently after a broker blip,
    /// so the cached instances stay valid across one.
    /// </summary>
    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            return _channel;

        await _connectionLock.WaitAsync(ct);
        try
        {
            // Re-check inside the lock: another caller may have already rebuilt while we waited.
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return _channel;

            if (_channel != null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not { IsOpen: true })
            {
                if (_connection != null)
                    await _connection.DisposeAsync();
                _connection = await _factory.CreateConnectionAsync(ct);
            }

            // Publisher confirms make BasicPublishAsync wait for the broker to actually accept the
            // message (and throw PublishException on a nack/basic.return) instead of returning as
            // soon as the frame is locally buffered, so PublishAsync's "enqueued" contract is real.
            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            _channel = await _connection.CreateChannelAsync(channelOptions, ct);

            // Arguments must match the Worker's declaration of the same queue exactly, or whichever
            // side declares second fails with PRECONDITION_FAILED (see QueueNames.CreateEmailQueueArguments).
            await _channel.QueueDeclareAsync(
                queue: QueueNames.Email,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: QueueNames.CreateEmailQueueArguments(),
                cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
