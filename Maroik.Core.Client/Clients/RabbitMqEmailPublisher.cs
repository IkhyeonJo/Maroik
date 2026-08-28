using System.Text.Json;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Messaging;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Publishes <see cref="SendEmailMessage"/> to the durable "maroik.email" queue.
/// The connection is opened lazily on first publish so a RabbitMQ outage at
/// application startup does not prevent the web app from starting.
/// </summary>
public sealed class RabbitMqEmailPublisher : IEmailPublisher, IAsyncDisposable
{
    private readonly IConnectionFactory _factory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    /// <summary>Initializes a new instance of <see cref="RabbitMqEmailPublisher"/>. Does not open a connection yet.</summary>
    public RabbitMqEmailPublisher(IConnectionFactory factory)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public async Task PublishAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        IConnection connection = await GetConnectionAsync(ct);

        // Publisher confirms make BasicPublishAsync wait for the broker to actually accept the
        // message (and throw PublishException on a nack/basic.return) instead of returning as
        // soon as the frame is locally buffered, so PublishAsync's "enqueued" contract is real.
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using IChannel channel = await connection.CreateChannelAsync(channelOptions, ct);
        // Arguments must match the Worker's declaration of the same queue exactly, or whichever
        // side declares second fails with PRECONDITION_FAILED (see QueueNames.CreateEmailQueueArguments).
        await channel.QueueDeclareAsync(
            queue: QueueNames.Email,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: QueueNames.CreateEmailQueueArguments(),
            cancellationToken: ct);

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
    /// Returns the current AMQP connection, opening (or reopening, if it dropped) one under a
    /// lock if necessary. Double-checked locking avoids taking the semaphore on the common path
    /// where the connection is already open.
    /// </summary>
    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(ct);
        try
        {
            // Re-check inside the lock: another caller may have already reconnected while we waited.
            if (_connection is { IsOpen: true })
                return _connection;

            if (_connection != null)
                await _connection.DisposeAsync();

            _connection = await _factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
            await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}
