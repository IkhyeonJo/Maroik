using RabbitMQ.Client;

namespace Maroik.Core.Client.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IConnectionFactory"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeConnectionFactory : IConnectionFactory
{
    /// <summary>The value <see cref="IConnectionFactory.ClientProperties"/> answers; settable by a test.</summary>
    public IDictionary<string, object?> ClientProperties { get; set; } = new Dictionary<string, object?>();

    /// <summary>The value <see cref="IConnectionFactory.Password"/> answers; settable by a test.</summary>
    public string Password { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionFactory.RequestedChannelMax"/> answers; settable by a test.</summary>
    public ushort RequestedChannelMax { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.RequestedFrameMax"/> answers; settable by a test.</summary>
    public uint RequestedFrameMax { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.RequestedHeartbeat"/> answers; settable by a test.</summary>
    public TimeSpan RequestedHeartbeat { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.UserName"/> answers; settable by a test.</summary>
    public string UserName { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionFactory.VirtualHost"/> answers; settable by a test.</summary>
    public string VirtualHost { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionFactory.CredentialsProvider"/> answers; settable by a test.</summary>
    public ICredentialsProvider? CredentialsProvider { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.Uri"/> answers; settable by a test.</summary>
    public Uri Uri { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionFactory.ClientProvidedName"/> answers; settable by a test.</summary>
    public string? ClientProvidedName { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.HandshakeContinuationTimeout"/> answers; settable by a test.</summary>
    public TimeSpan HandshakeContinuationTimeout { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.ContinuationTimeout"/> answers; settable by a test.</summary>
    public TimeSpan ContinuationTimeout { get; set; }

    /// <summary>The value <see cref="IConnectionFactory.ConsumerDispatchConcurrency"/> answers; settable by a test.</summary>
    public ushort ConsumerDispatchConcurrency { get; set; }

    /// <summary>What <c>AuthMechanismFactory</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<string>, IAuthMechanismFactory?>? OnAuthMechanismFactory { get; set; }

    /// <summary>The arguments of every <c>AuthMechanismFactory</c> call, in order.</summary>
    public List<IEnumerable<string>> AuthMechanismFactoryCalls { get; } = [];

    /// <inheritdoc/>
    public IAuthMechanismFactory? AuthMechanismFactory(IEnumerable<string> mechanismNames)
    {
        AuthMechanismFactoryCalls.Add(mechanismNames);
        if (OnAuthMechanismFactory is { } on)
        {
            return on(mechanismNames);
        }

        return default;
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CancellationToken, Task<IConnection>>? OnCreateConnectionAsync { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<CancellationToken> CreateConnectionAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        CreateConnectionAsyncCalls.Add(cancellationToken);
        if (OnCreateConnectionAsync is { } on)
        {
            return on(cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<IConnection>>? OnCreateConnectionAsync_ClientProvidedName { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<(string ClientProvidedName, CancellationToken CancellationToken)> CreateConnectionAsync_ClientProvidedNameCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(string clientProvidedName, CancellationToken cancellationToken)
    {
        CreateConnectionAsync_ClientProvidedNameCalls.Add((clientProvidedName, cancellationToken));
        if (OnCreateConnectionAsync_ClientProvidedName is { } on)
        {
            return on(clientProvidedName, cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<string>, CancellationToken, Task<IConnection>>? OnCreateConnectionAsync_Hostnames { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<(IEnumerable<string> Hostnames, CancellationToken CancellationToken)> CreateConnectionAsync_HostnamesCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, CancellationToken cancellationToken)
    {
        CreateConnectionAsync_HostnamesCalls.Add((hostnames, cancellationToken));
        if (OnCreateConnectionAsync_Hostnames is { } on)
        {
            return on(hostnames, cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<string>, string, CancellationToken, Task<IConnection>>? OnCreateConnectionAsync_Hostnames_ClientProvidedName { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<(IEnumerable<string> Hostnames, string ClientProvidedName, CancellationToken CancellationToken)> CreateConnectionAsync_Hostnames_ClientProvidedNameCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(IEnumerable<string> hostnames, string clientProvidedName, CancellationToken cancellationToken)
    {
        CreateConnectionAsync_Hostnames_ClientProvidedNameCalls.Add((hostnames, clientProvidedName, cancellationToken));
        if (OnCreateConnectionAsync_Hostnames_ClientProvidedName is { } on)
        {
            return on(hostnames, clientProvidedName, cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<AmqpTcpEndpoint>, CancellationToken, Task<IConnection>>? OnCreateConnectionAsync_Endpoints { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<(IEnumerable<AmqpTcpEndpoint> Endpoints, CancellationToken CancellationToken)> CreateConnectionAsync_EndpointsCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, CancellationToken cancellationToken)
    {
        CreateConnectionAsync_EndpointsCalls.Add((endpoints, cancellationToken));
        if (OnCreateConnectionAsync_Endpoints is { } on)
        {
            return on(endpoints, cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }

    /// <summary>What <c>CreateConnectionAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IEnumerable<AmqpTcpEndpoint>, string, CancellationToken, Task<IConnection>>? OnCreateConnectionAsync_Endpoints_ClientProvidedName { get; set; }

    /// <summary>The arguments of every <c>CreateConnectionAsync</c> call, in order.</summary>
    public List<(IEnumerable<AmqpTcpEndpoint> Endpoints, string ClientProvidedName, CancellationToken CancellationToken)> CreateConnectionAsync_Endpoints_ClientProvidedNameCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IConnection> CreateConnectionAsync(IEnumerable<AmqpTcpEndpoint> endpoints, string clientProvidedName, CancellationToken cancellationToken)
    {
        CreateConnectionAsync_Endpoints_ClientProvidedNameCalls.Add((endpoints, clientProvidedName, cancellationToken));
        if (OnCreateConnectionAsync_Endpoints_ClientProvidedName is { } on)
        {
            return on(endpoints, clientProvidedName, cancellationToken);
        }

        return Task.FromResult<IConnection>(default!);
    }
}
