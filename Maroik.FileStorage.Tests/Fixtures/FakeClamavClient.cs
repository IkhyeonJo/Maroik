using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.FileStorage.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IClamavClient"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeClamavClient : IClamavClient
{
    /// <summary>What <c>ScanWithClamavAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Stream, string, int, CancellationToken, Task<ClamavScanResult>>? OnScanWithClamavAsync { get; set; }

    /// <summary>The arguments of every <c>ScanWithClamavAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Stream FileStream, string ClamavHost, int ClamavPort, CancellationToken Ct)> ScanWithClamavAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ClamavScanResult> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, CancellationToken ct)
    {
        lock (ScanWithClamavAsyncCalls) ScanWithClamavAsyncCalls.Add((fileStream, clamavHost, clamavPort, ct));
        if (OnScanWithClamavAsync is { } on)
        {
            return on(fileStream, clamavHost, clamavPort, ct);
        }

        return Task.FromResult<ClamavScanResult>(default);
    }

    /// <summary>What <c>ScanWithClamavAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Stream, string, int, TimeSpan, CancellationToken, Task<ClamavScanResult>>? OnScanWithClamavAsyncWithScanTimeout { get; set; }

    /// <summary>The arguments of every <c>ScanWithClamavAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Stream FileStream, string ClamavHost, int ClamavPort, TimeSpan ScanTimeout, CancellationToken Ct)> ScanWithClamavAsyncWithScanTimeoutCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ClamavScanResult> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort, TimeSpan scanTimeout, CancellationToken ct)
    {
        lock (ScanWithClamavAsyncWithScanTimeoutCalls) ScanWithClamavAsyncWithScanTimeoutCalls.Add((fileStream, clamavHost, clamavPort, scanTimeout, ct));
        if (OnScanWithClamavAsyncWithScanTimeout is { } on)
        {
            return on(fileStream, clamavHost, clamavPort, scanTimeout, ct);
        }

        return Task.FromResult<ClamavScanResult>(default);
    }
}
