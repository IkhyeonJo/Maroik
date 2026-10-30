using System.Reflection;
using StackExchange.Redis;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// A hand-written stand-in for <see cref="IDatabase"/> that fakes <c>PingAsync</c> only (the one call the Valkey
/// health check makes). <see cref="IDatabase"/> has several hundred members, so instead of spelling each out this is a
/// <see cref="DispatchProxy"/>: <see cref="Create"/> returns an <see cref="IDatabase"/> whose calls land in <see cref="Invoke"/>, where
/// any other member throws <see cref="NotSupportedException"/>. (A <see cref="DispatchProxy"/> type must not be sealed.)
/// </summary>
public class FakeDatabase : DispatchProxy
{
    /// <summary>What <c>PingAsync</c> does; <see langword="null"/> answers a zero round trip.</summary>
    public Func<CommandFlags, Task<TimeSpan>>? OnPingAsync { get; set; }

    /// <summary>The flags of every <c>PingAsync</c> call, in order.</summary>
    public List<CommandFlags> PingAsyncCalls { get; } = [];

    /// <summary>A new fake, as the <see cref="IDatabase"/> handed to the code under test and the <see cref="FakeDatabase"/> a test configures.</summary>
    public static (IDatabase Database, FakeDatabase Fake) Create()
    {
        IDatabase database = Create<IDatabase, FakeDatabase>();
        return (database, (FakeDatabase)(object)database);
    }

    /// <inheritdoc/>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name != nameof(IDatabaseAsync.PingAsync) || args is not [CommandFlags flags])
            throw new NotSupportedException($"{targetMethod?.Name} is not faked.");

        lock (PingAsyncCalls) PingAsyncCalls.Add(flags);
        return OnPingAsync is { } on ? on(flags) : Task.FromResult(TimeSpan.Zero);
    }
}
