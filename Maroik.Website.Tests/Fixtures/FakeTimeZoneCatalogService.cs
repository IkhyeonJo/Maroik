using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ITimeZoneCatalogService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeTimeZoneCatalogService : ITimeZoneCatalogService
{
    /// <summary>What <c>GetTimeZoneOptions</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IReadOnlyList<TimeZoneOptionDto>>? OnGetTimeZoneOptions { get; set; }

    /// <summary>How many times <c>GetTimeZoneOptions</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int GetTimeZoneOptionsCallCount => Volatile.Read(ref _getTimeZoneOptionsCallCount);

    /// <summary>Backs <see cref="GetTimeZoneOptionsCallCount"/>.</summary>
    private int _getTimeZoneOptionsCallCount;

    /// <inheritdoc/>
    public IReadOnlyList<TimeZoneOptionDto> GetTimeZoneOptions()
    {
        Interlocked.Increment(ref _getTimeZoneOptionsCallCount);
        if (OnGetTimeZoneOptions is { } on)
        {
            return on();
        }

        return [];
    }
}
