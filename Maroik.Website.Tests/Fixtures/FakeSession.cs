using Microsoft.AspNetCore.Http;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// A hand-written in-memory <see cref="ISession"/>: <see cref="Values"/> holds what was set (a test may seed it), and every
/// <see cref="Set"/> / <see cref="Remove"/> is recorded.
/// </summary>
public sealed class FakeSession : ISession
{
    /// <summary>The stored values by key.</summary>
    public Dictionary<string, byte[]> Values { get; } = [];

    /// <summary>The key and value of every <see cref="Set"/> call, in order.</summary>
    public List<(string Key, byte[] Value)> SetCalls { get; } = [];

    /// <summary>The key of every <see cref="Remove"/> call, in order.</summary>
    public List<string> RemoveCalls { get; } = [];

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public string Id { get; } = Guid.NewGuid().ToString("N");

    /// <inheritdoc/>
    public IEnumerable<string> Keys => Values.Keys;

    /// <inheritdoc/>
    public void Clear() => Values.Clear();

    /// <inheritdoc/>
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc/>
    public void Remove(string key)
    {
        RemoveCalls.Add(key);
        Values.Remove(key);
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value)
    {
        SetCalls.Add((key, value));
        Values[key] = value;
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, out byte[] value)
    {
        if (Values.TryGetValue(key, out byte[]? stored))
        {
            value = stored;
            return true;
        }

        value = [];
        return false;
    }
}
