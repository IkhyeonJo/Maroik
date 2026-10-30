using Maroik.Website.HealthChecks;
using Maroik.Website.Tests.Fixtures;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Maroik.Website.Tests.HealthChecks;

/// <summary>
/// Unit tests for <see cref="ValkeyHealthCheck"/>. <see cref="IConnectionMultiplexer"/> and
/// <see cref="IDatabase"/> are replaced with hand-written fakes so both the reachable and unreachable
/// paths can be exercised without a real Valkey instance.
/// </summary>
public sealed class ValkeyHealthCheckTests : IDisposable
{
    /// <summary>Fake <c>IConnectionMultiplexer</c> injected into the system under test; hands out <see cref="_database"/>.</summary>
    private readonly FakeConnectionMultiplexer _connectionMultiplexer = new();
    /// <summary>Fake <c>IDatabase</c> the multiplexer hands out.</summary>
    private readonly FakeDatabase _database;

    /// <summary>Wires the multiplexer to hand out the fake database.</summary>
    public ValkeyHealthCheckTests()
    {
        (IDatabase database, _database) = FakeDatabase.Create();
        _connectionMultiplexer.OnGetDatabase = (_, _) => database;
    }

    /// <inheritdoc />
    public void Dispose() => _connectionMultiplexer.Dispose();

    /// <summary>The health check under test over the fake connection.</summary>
    private ValkeyHealthCheck CreateSut() => new(_connectionMultiplexer);

    /// <summary>Verifies a successful PING is reported as healthy.</summary>
    [Fact]
    public async Task CheckHealthAsync_PingSucceeds_ReturnsHealthy()
    {
        _database.OnPingAsync = _ => Task.FromResult(TimeSpan.FromMilliseconds(1));
        var sut = CreateSut();

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Single(_database.PingAsyncCalls);
    }

    /// <summary>Verifies a connection failure is reported as unhealthy rather than throwing.</summary>
    [Fact]
    public async Task CheckHealthAsync_PingThrows_ReturnsUnhealthy()
    {
        var failure = new RedisConnectionException(ConnectionFailureType.UnableToConnect, "unreachable");
        _database.OnPingAsync = _ => Task.FromException<TimeSpan>(failure);
        var sut = CreateSut();

        HealthCheckResult result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Same(failure, result.Exception);
    }
}
