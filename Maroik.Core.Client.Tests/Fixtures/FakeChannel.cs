using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maroik.Core.Client.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IChannel"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeChannel : IChannel
{
    /// <summary>The value <see cref="IChannel.ChannelNumber"/> answers; settable by a test.</summary>
    public int ChannelNumber { get; set; }

    /// <summary>The value <see cref="IChannel.CloseReason"/> answers; settable by a test.</summary>
    public ShutdownEventArgs? CloseReason { get; set; }

    /// <summary>The value <see cref="IChannel.DefaultConsumer"/> answers; settable by a test.</summary>
    public IAsyncBasicConsumer? DefaultConsumer { get; set; }

    /// <summary>The value <see cref="IChannel.IsClosed"/> answers; settable by a test.</summary>
    public bool IsClosed { get; set; }

    /// <summary>The value <see cref="IChannel.IsOpen"/> answers; settable by a test.</summary>
    public bool IsOpen { get; set; }

    /// <summary>The value <see cref="IChannel.CurrentQueue"/> answers; settable by a test.</summary>
    public string? CurrentQueue { get; set; }

    /// <summary>The value <see cref="IChannel.ContinuationTimeout"/> answers; settable by a test.</summary>
    public TimeSpan ContinuationTimeout { get; set; }

    /// <summary>Subscribers to <c>BasicAcksAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<BasicAckEventArgs>> BasicAcksAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<BasicAckEventArgs> BasicAcksAsync { add => BasicAcksAsyncHandlers.Add(value); remove => BasicAcksAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>BasicNacksAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<BasicNackEventArgs>> BasicNacksAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<BasicNackEventArgs> BasicNacksAsync { add => BasicNacksAsyncHandlers.Add(value); remove => BasicNacksAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>BasicReturnAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<BasicReturnEventArgs>> BasicReturnAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<BasicReturnEventArgs> BasicReturnAsync { add => BasicReturnAsyncHandlers.Add(value); remove => BasicReturnAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>CallbackExceptionAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<CallbackExceptionEventArgs>> CallbackExceptionAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync { add => CallbackExceptionAsyncHandlers.Add(value); remove => CallbackExceptionAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>FlowControlAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<FlowControlEventArgs>> FlowControlAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<FlowControlEventArgs> FlowControlAsync { add => FlowControlAsyncHandlers.Add(value); remove => FlowControlAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ChannelShutdownAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<ShutdownEventArgs>> ChannelShutdownAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<ShutdownEventArgs> ChannelShutdownAsync { add => ChannelShutdownAsyncHandlers.Add(value); remove => ChannelShutdownAsyncHandlers.Remove(value); }

    /// <summary>What <c>GetNextPublishSequenceNumberAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, ValueTask<ulong>>? OnGetNextPublishSequenceNumberAsync { get; set; }

    /// <summary>The arguments of every <c>GetNextPublishSequenceNumberAsync</c> call, in order.</summary>
    public List<CancellationToken> GetNextPublishSequenceNumberAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public ValueTask<ulong> GetNextPublishSequenceNumberAsync(CancellationToken cancellationToken)
    {
        GetNextPublishSequenceNumberAsyncCalls.Add(cancellationToken);
        if (OnGetNextPublishSequenceNumberAsync is { } on)
        {
            return on(cancellationToken);
        }

        return ValueTask.FromResult<ulong>(default);
    }

    /// <summary>What <c>BasicAckAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ulong, bool, CancellationToken, ValueTask>? OnBasicAckAsync { get; set; }

    /// <summary>The arguments of every <c>BasicAckAsync</c> call, in order.</summary>
    public List<(ulong DeliveryTag, bool Multiple, CancellationToken CancellationToken)> BasicAckAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public ValueTask BasicAckAsync(ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
    {
        BasicAckAsyncCalls.Add((deliveryTag, multiple, cancellationToken));
        if (OnBasicAckAsync is { } on)
        {
            return on(deliveryTag, multiple, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>What <c>BasicNackAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ulong, bool, bool, CancellationToken, ValueTask>? OnBasicNackAsync { get; set; }

    /// <summary>The arguments of every <c>BasicNackAsync</c> call, in order.</summary>
    public List<(ulong DeliveryTag, bool Multiple, bool Requeue, CancellationToken CancellationToken)> BasicNackAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public ValueTask BasicNackAsync(ulong deliveryTag, bool multiple, bool requeue, CancellationToken cancellationToken)
    {
        BasicNackAsyncCalls.Add((deliveryTag, multiple, requeue, cancellationToken));
        if (OnBasicNackAsync is { } on)
        {
            return on(deliveryTag, multiple, requeue, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>What <c>BasicCancelAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, CancellationToken, Task>? OnBasicCancelAsync { get; set; }

    /// <summary>The arguments of every <c>BasicCancelAsync</c> call, in order.</summary>
    public List<(string ConsumerTag, bool NoWait, CancellationToken CancellationToken)> BasicCancelAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task BasicCancelAsync(string consumerTag, bool noWait, CancellationToken cancellationToken)
    {
        BasicCancelAsyncCalls.Add((consumerTag, noWait, cancellationToken));
        if (OnBasicCancelAsync is { } on)
        {
            return on(consumerTag, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>BasicConsumeAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, string, bool, bool, IDictionary<string, object?>?, IAsyncBasicConsumer, CancellationToken, Task<string>>? OnBasicConsumeAsync { get; set; }

    /// <summary>The arguments of every <c>BasicConsumeAsync</c> call, in order.</summary>
    public List<(string Queue, bool AutoAck, string ConsumerTag, bool NoLocal, bool Exclusive, IDictionary<string, object?>? Arguments, IAsyncBasicConsumer Consumer, CancellationToken CancellationToken)> BasicConsumeAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<string> BasicConsumeAsync(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object?>? arguments, IAsyncBasicConsumer consumer, CancellationToken cancellationToken)
    {
        BasicConsumeAsyncCalls.Add((queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, cancellationToken));
        if (OnBasicConsumeAsync is { } on)
        {
            return on(queue, autoAck, consumerTag, noLocal, exclusive, arguments, consumer, cancellationToken);
        }

        return Task.FromResult<string>(default!);
    }

    /// <summary>What <c>BasicGetAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, CancellationToken, Task<BasicGetResult?>>? OnBasicGetAsync { get; set; }

    /// <summary>The arguments of every <c>BasicGetAsync</c> call, in order.</summary>
    public List<(string Queue, bool AutoAck, CancellationToken CancellationToken)> BasicGetAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<BasicGetResult?> BasicGetAsync(string queue, bool autoAck, CancellationToken cancellationToken)
    {
        BasicGetAsyncCalls.Add((queue, autoAck, cancellationToken));
        if (OnBasicGetAsync is { } on)
        {
            return on(queue, autoAck, cancellationToken);
        }

        return Task.FromResult<BasicGetResult?>(default);
    }

    /// <summary>
    /// What <c>BasicPublishAsync</c> does with each publish (exchange, routing key, mandatory, properties, body);
    /// <see langword="null"/> completes at once.
    /// </summary>
    public Func<string, string, bool, IReadOnlyBasicProperties, ReadOnlyMemory<byte>, CancellationToken, ValueTask>? OnBasicPublishAsync { get; set; }

    /// <summary>Every publish, in order (the body is copied, since the caller may reuse its buffer).</summary>
    public List<(string Exchange, string RoutingKey, bool Mandatory, IReadOnlyBasicProperties BasicProperties, byte[] Body, CancellationToken CancellationToken)> BasicPublishAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public ValueTask BasicPublishAsync<TProperties>(string exchange, string routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
        where TProperties : IReadOnlyBasicProperties, IAmqpHeader
    {
        BasicPublishAsyncCalls.Add((exchange, routingKey, mandatory, basicProperties, body.ToArray(), cancellationToken));
        return OnBasicPublishAsync is { } on ? on(exchange, routingKey, mandatory, basicProperties, body, cancellationToken) : ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask BasicPublishAsync<TProperties>(CachedString exchange, CachedString routingKey, bool mandatory, TProperties basicProperties, ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
        where TProperties : IReadOnlyBasicProperties, IAmqpHeader
        => BasicPublishAsync(exchange.Value, routingKey.Value, mandatory, basicProperties, body, cancellationToken);

    /// <summary>What <c>BasicQosAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<uint, ushort, bool, CancellationToken, Task>? OnBasicQosAsync { get; set; }

    /// <summary>The arguments of every <c>BasicQosAsync</c> call, in order.</summary>
    public List<(uint PrefetchSize, ushort PrefetchCount, bool Global, CancellationToken CancellationToken)> BasicQosAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task BasicQosAsync(uint prefetchSize, ushort prefetchCount, bool global, CancellationToken cancellationToken)
    {
        BasicQosAsyncCalls.Add((prefetchSize, prefetchCount, global, cancellationToken));
        if (OnBasicQosAsync is { } on)
        {
            return on(prefetchSize, prefetchCount, global, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>BasicRejectAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ulong, bool, CancellationToken, ValueTask>? OnBasicRejectAsync { get; set; }

    /// <summary>The arguments of every <c>BasicRejectAsync</c> call, in order.</summary>
    public List<(ulong DeliveryTag, bool Requeue, CancellationToken CancellationToken)> BasicRejectAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public ValueTask BasicRejectAsync(ulong deliveryTag, bool requeue, CancellationToken cancellationToken)
    {
        BasicRejectAsyncCalls.Add((deliveryTag, requeue, cancellationToken));
        if (OnBasicRejectAsync is { } on)
        {
            return on(deliveryTag, requeue, cancellationToken);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>What <c>CloseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ushort, string, bool, CancellationToken, Task>? OnCloseAsync_ReplyCode_ReplyText_Abort { get; set; }

    /// <summary>The arguments of every <c>CloseAsync</c> call, in order.</summary>
    public List<(ushort ReplyCode, string ReplyText, bool Abort, CancellationToken CancellationToken)> CloseAsync_ReplyCode_ReplyText_AbortCalls { get; } = [];

    /// <inheritdoc/>
    public Task CloseAsync(ushort replyCode, string replyText, bool abort, CancellationToken cancellationToken)
    {
        CloseAsync_ReplyCode_ReplyText_AbortCalls.Add((replyCode, replyText, abort, cancellationToken));
        if (OnCloseAsync_ReplyCode_ReplyText_Abort is { } on)
        {
            return on(replyCode, replyText, abort, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>CloseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ShutdownEventArgs, bool, Task>? OnCloseAsync_Reason_Abort { get; set; }

    /// <summary>The arguments of every <c>CloseAsync</c> call, in order.</summary>
    public List<(ShutdownEventArgs Reason, bool Abort)> CloseAsync_Reason_AbortCalls { get; } = [];

    /// <inheritdoc/>
    public Task CloseAsync(ShutdownEventArgs reason, bool abort)
    {
        CloseAsync_Reason_AbortCalls.Add((reason, abort));
        if (OnCloseAsync_Reason_Abort is { } on)
        {
            return on(reason, abort);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>CloseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ShutdownEventArgs, bool, CancellationToken, Task>? OnCloseAsync_Reason_AbortX { get; set; }

    /// <summary>The arguments of every <c>CloseAsync</c> call, in order.</summary>
    public List<(ShutdownEventArgs Reason, bool Abort, CancellationToken CancellationToken)> CloseAsync_Reason_AbortXCalls { get; } = [];

    /// <inheritdoc/>
    public Task CloseAsync(ShutdownEventArgs reason, bool abort, CancellationToken cancellationToken)
    {
        CloseAsync_Reason_AbortXCalls.Add((reason, abort, cancellationToken));
        if (OnCloseAsync_Reason_AbortX is { } on)
        {
            return on(reason, abort, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ExchangeDeclareAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, bool, bool, IDictionary<string, object?>?, bool, bool, CancellationToken, Task>? OnExchangeDeclareAsync { get; set; }

    /// <summary>The arguments of every <c>ExchangeDeclareAsync</c> call, in order.</summary>
    public List<(string Exchange, string Type, bool Durable, bool AutoDelete, IDictionary<string, object?>? Arguments, bool Passive, bool NoWait, CancellationToken CancellationToken)> ExchangeDeclareAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ExchangeDeclareAsync(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object?>? arguments, bool passive, bool noWait, CancellationToken cancellationToken)
    {
        ExchangeDeclareAsyncCalls.Add((exchange, type, durable, autoDelete, arguments, passive, noWait, cancellationToken));
        if (OnExchangeDeclareAsync is { } on)
        {
            return on(exchange, type, durable, autoDelete, arguments, passive, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ExchangeDeclarePassiveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task>? OnExchangeDeclarePassiveAsync { get; set; }

    /// <summary>The arguments of every <c>ExchangeDeclarePassiveAsync</c> call, in order.</summary>
    public List<(string Exchange, CancellationToken CancellationToken)> ExchangeDeclarePassiveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ExchangeDeclarePassiveAsync(string exchange, CancellationToken cancellationToken)
    {
        ExchangeDeclarePassiveAsyncCalls.Add((exchange, cancellationToken));
        if (OnExchangeDeclarePassiveAsync is { } on)
        {
            return on(exchange, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ExchangeDeleteAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, bool, CancellationToken, Task>? OnExchangeDeleteAsync { get; set; }

    /// <summary>The arguments of every <c>ExchangeDeleteAsync</c> call, in order.</summary>
    public List<(string Exchange, bool IfUnused, bool NoWait, CancellationToken CancellationToken)> ExchangeDeleteAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ExchangeDeleteAsync(string exchange, bool ifUnused, bool noWait, CancellationToken cancellationToken)
    {
        ExchangeDeleteAsyncCalls.Add((exchange, ifUnused, noWait, cancellationToken));
        if (OnExchangeDeleteAsync is { } on)
        {
            return on(exchange, ifUnused, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ExchangeBindAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, IDictionary<string, object?>?, bool, CancellationToken, Task>? OnExchangeBindAsync { get; set; }

    /// <summary>The arguments of every <c>ExchangeBindAsync</c> call, in order.</summary>
    public List<(string Destination, string Source, string RoutingKey, IDictionary<string, object?>? Arguments, bool NoWait, CancellationToken CancellationToken)> ExchangeBindAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ExchangeBindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments, bool noWait, CancellationToken cancellationToken)
    {
        ExchangeBindAsyncCalls.Add((destination, source, routingKey, arguments, noWait, cancellationToken));
        if (OnExchangeBindAsync is { } on)
        {
            return on(destination, source, routingKey, arguments, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>ExchangeUnbindAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, IDictionary<string, object?>?, bool, CancellationToken, Task>? OnExchangeUnbindAsync { get; set; }

    /// <summary>The arguments of every <c>ExchangeUnbindAsync</c> call, in order.</summary>
    public List<(string Destination, string Source, string RoutingKey, IDictionary<string, object?>? Arguments, bool NoWait, CancellationToken CancellationToken)> ExchangeUnbindAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task ExchangeUnbindAsync(string destination, string source, string routingKey, IDictionary<string, object?>? arguments, bool noWait, CancellationToken cancellationToken)
    {
        ExchangeUnbindAsyncCalls.Add((destination, source, routingKey, arguments, noWait, cancellationToken));
        if (OnExchangeUnbindAsync is { } on)
        {
            return on(destination, source, routingKey, arguments, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>QueueDeclareAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, bool, bool, IDictionary<string, object?>?, bool, bool, CancellationToken, Task<QueueDeclareOk>>? OnQueueDeclareAsync { get; set; }

    /// <summary>The arguments of every <c>QueueDeclareAsync</c> call, in order.</summary>
    public List<(string Queue, bool Durable, bool Exclusive, bool AutoDelete, IDictionary<string, object?>? Arguments, bool Passive, bool NoWait, CancellationToken CancellationToken)> QueueDeclareAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<QueueDeclareOk> QueueDeclareAsync(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object?>? arguments, bool passive, bool noWait, CancellationToken cancellationToken)
    {
        QueueDeclareAsyncCalls.Add((queue, durable, exclusive, autoDelete, arguments, passive, noWait, cancellationToken));
        if (OnQueueDeclareAsync is { } on)
        {
            return on(queue, durable, exclusive, autoDelete, arguments, passive, noWait, cancellationToken);
        }

        return Task.FromResult<QueueDeclareOk>(default!);
    }

    /// <summary>What <c>QueueDeclarePassiveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<QueueDeclareOk>>? OnQueueDeclarePassiveAsync { get; set; }

    /// <summary>The arguments of every <c>QueueDeclarePassiveAsync</c> call, in order.</summary>
    public List<(string Queue, CancellationToken CancellationToken)> QueueDeclarePassiveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<QueueDeclareOk> QueueDeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        QueueDeclarePassiveAsyncCalls.Add((queue, cancellationToken));
        if (OnQueueDeclarePassiveAsync is { } on)
        {
            return on(queue, cancellationToken);
        }

        return Task.FromResult<QueueDeclareOk>(default!);
    }

    /// <summary>What <c>QueueDeleteAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, bool, bool, bool, CancellationToken, Task<uint>>? OnQueueDeleteAsync { get; set; }

    /// <summary>The arguments of every <c>QueueDeleteAsync</c> call, in order.</summary>
    public List<(string Queue, bool IfUnused, bool IfEmpty, bool NoWait, CancellationToken CancellationToken)> QueueDeleteAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<uint> QueueDeleteAsync(string queue, bool ifUnused, bool ifEmpty, bool noWait, CancellationToken cancellationToken)
    {
        QueueDeleteAsyncCalls.Add((queue, ifUnused, ifEmpty, noWait, cancellationToken));
        if (OnQueueDeleteAsync is { } on)
        {
            return on(queue, ifUnused, ifEmpty, noWait, cancellationToken);
        }

        return Task.FromResult<uint>(default);
    }

    /// <summary>What <c>QueuePurgeAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<uint>>? OnQueuePurgeAsync { get; set; }

    /// <summary>The arguments of every <c>QueuePurgeAsync</c> call, in order.</summary>
    public List<(string Queue, CancellationToken CancellationToken)> QueuePurgeAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<uint> QueuePurgeAsync(string queue, CancellationToken cancellationToken)
    {
        QueuePurgeAsyncCalls.Add((queue, cancellationToken));
        if (OnQueuePurgeAsync is { } on)
        {
            return on(queue, cancellationToken);
        }

        return Task.FromResult<uint>(default);
    }

    /// <summary>What <c>QueueBindAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, IDictionary<string, object?>?, bool, CancellationToken, Task>? OnQueueBindAsync { get; set; }

    /// <summary>The arguments of every <c>QueueBindAsync</c> call, in order.</summary>
    public List<(string Queue, string Exchange, string RoutingKey, IDictionary<string, object?>? Arguments, bool NoWait, CancellationToken CancellationToken)> QueueBindAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task QueueBindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments, bool noWait, CancellationToken cancellationToken)
    {
        QueueBindAsyncCalls.Add((queue, exchange, routingKey, arguments, noWait, cancellationToken));
        if (OnQueueBindAsync is { } on)
        {
            return on(queue, exchange, routingKey, arguments, noWait, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>QueueUnbindAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, IDictionary<string, object?>?, CancellationToken, Task>? OnQueueUnbindAsync { get; set; }

    /// <summary>The arguments of every <c>QueueUnbindAsync</c> call, in order.</summary>
    public List<(string Queue, string Exchange, string RoutingKey, IDictionary<string, object?>? Arguments, CancellationToken CancellationToken)> QueueUnbindAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task QueueUnbindAsync(string queue, string exchange, string routingKey, IDictionary<string, object?>? arguments, CancellationToken cancellationToken)
    {
        QueueUnbindAsyncCalls.Add((queue, exchange, routingKey, arguments, cancellationToken));
        if (OnQueueUnbindAsync is { } on)
        {
            return on(queue, exchange, routingKey, arguments, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>MessageCountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<uint>>? OnMessageCountAsync { get; set; }

    /// <summary>The arguments of every <c>MessageCountAsync</c> call, in order.</summary>
    public List<(string Queue, CancellationToken CancellationToken)> MessageCountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<uint> MessageCountAsync(string queue, CancellationToken cancellationToken)
    {
        MessageCountAsyncCalls.Add((queue, cancellationToken));
        if (OnMessageCountAsync is { } on)
        {
            return on(queue, cancellationToken);
        }

        return Task.FromResult<uint>(default);
    }

    /// <summary>What <c>ConsumerCountAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<uint>>? OnConsumerCountAsync { get; set; }

    /// <summary>The arguments of every <c>ConsumerCountAsync</c> call, in order.</summary>
    public List<(string Queue, CancellationToken CancellationToken)> ConsumerCountAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<uint> ConsumerCountAsync(string queue, CancellationToken cancellationToken)
    {
        ConsumerCountAsyncCalls.Add((queue, cancellationToken));
        if (OnConsumerCountAsync is { } on)
        {
            return on(queue, cancellationToken);
        }

        return Task.FromResult<uint>(default);
    }

    /// <summary>What <c>TxCommitAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task>? OnTxCommitAsync { get; set; }

    /// <summary>The arguments of every <c>TxCommitAsync</c> call, in order.</summary>
    public List<CancellationToken> TxCommitAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task TxCommitAsync(CancellationToken cancellationToken)
    {
        TxCommitAsyncCalls.Add(cancellationToken);
        if (OnTxCommitAsync is { } on)
        {
            return on(cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>TxRollbackAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task>? OnTxRollbackAsync { get; set; }

    /// <summary>The arguments of every <c>TxRollbackAsync</c> call, in order.</summary>
    public List<CancellationToken> TxRollbackAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task TxRollbackAsync(CancellationToken cancellationToken)
    {
        TxRollbackAsyncCalls.Add(cancellationToken);
        if (OnTxRollbackAsync is { } on)
        {
            return on(cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>TxSelectAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task>? OnTxSelectAsync { get; set; }

    /// <summary>The arguments of every <c>TxSelectAsync</c> call, in order.</summary>
    public List<CancellationToken> TxSelectAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task TxSelectAsync(CancellationToken cancellationToken)
    {
        TxSelectAsyncCalls.Add(cancellationToken);
        if (OnTxSelectAsync is { } on)
        {
            return on(cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>DisposeAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ValueTask>? OnDisposeAsync { get; set; }

    /// <summary>How many times <c>DisposeAsync</c> was called.</summary>
    public int DisposeAsyncCallCount { get; private set; }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        DisposeAsyncCallCount++;
        if (OnDisposeAsync is { } on)
        {
            return on();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>What <c>Dispose</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action? OnDispose { get; set; }

    /// <summary>How many times <c>Dispose</c> was called.</summary>
    public int DisposeCallCount { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        DisposeCallCount++;
        OnDispose?.Invoke();
    }
}
