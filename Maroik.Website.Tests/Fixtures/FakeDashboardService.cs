using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IDashboardService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeDashboardService : IDashboardService
{
    /// <summary>What <c>GetSummaryAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, CancellationToken, Task<DashboardDto>>? OnGetSummaryAsync { get; set; }

    /// <summary>The arguments of every <c>GetSummaryAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string Year, string Month, string TimeZoneId, CancellationToken Ct)> GetSummaryAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<DashboardDto> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId, CancellationToken ct)
    {
        lock (GetSummaryAsyncCalls) GetSummaryAsyncCalls.Add((accountEmail, year, month, timeZoneId, ct));
        if (OnGetSummaryAsync is { } on)
        {
            return on(accountEmail, year, month, timeZoneId, ct);
        }

        return Task.FromResult<DashboardDto>(default!);
    }

    /// <summary>What <c>GetNoticeCountsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, int, string, CancellationToken, Task<NotificationDto>>? OnGetNoticeCountsAsync { get; set; }

    /// <summary>The arguments of every <c>GetNoticeCountsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Email, int NoticeMaturityDateDay, string TimeZoneId, CancellationToken Ct)> GetNoticeCountsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<NotificationDto> GetNoticeCountsAsync(string email, int noticeMaturityDateDay, string timeZoneId, CancellationToken ct)
    {
        lock (GetNoticeCountsAsyncCalls) GetNoticeCountsAsyncCalls.Add((email, noticeMaturityDateDay, timeZoneId, ct));
        if (OnGetNoticeCountsAsync is { } on)
        {
            return on(email, noticeMaturityDateDay, timeZoneId, ct);
        }

        return Task.FromResult<NotificationDto>(default!);
    }

    /// <summary>What <c>GetServerResourceSummary</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, ServerResourceDto>? OnGetServerResourceSummary { get; set; }

    /// <summary>The arguments of every <c>GetServerResourceSummary</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string HostResourceFilePath, string DockerResourceFilePath)> GetServerResourceSummaryCalls { get; } = [];

    /// <inheritdoc/>
    public ServerResourceDto GetServerResourceSummary(string hostResourceFilePath, string dockerResourceFilePath)
    {
        lock (GetServerResourceSummaryCalls) GetServerResourceSummaryCalls.Add((hostResourceFilePath, dockerResourceFilePath));
        if (OnGetServerResourceSummary is { } on)
        {
            return on(hostResourceFilePath, dockerResourceFilePath);
        }

        return default!;
    }
}
