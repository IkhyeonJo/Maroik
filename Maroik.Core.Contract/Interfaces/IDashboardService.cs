using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for the user dashboard, aggregating income, expenditure, and asset data
/// and computing notification badge counts.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Builds the full dashboard summary for the given account, year, and month.
    /// All date comparisons are performed in the user's <paramref name="timeZoneId"/>.
    /// </summary>
    Task<DashboardSummary> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId);

    /// <summary>Persists the user's preferred currency unit selection from the dashboard filter.</summary>
    Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit);

    /// <summary>
    /// Counts fixed income and expenditure items that are approaching their deposit day
    /// within <paramref name="noticeMaturityDateDay"/> days, or whose maturity date has passed.
    /// Used to populate the navigation header badges.
    /// </summary>
    Task<NotificationCounts> GetNoticeCountsAsync(string email, int noticeMaturityDateDay);

    /// <summary>
    /// Reads the host and Docker resource log files and returns the parsed metrics
    /// used by the admin dashboard.
    /// </summary>
    ServerResourceSummary GetServerResourceSummary(string hostResourceFilePath, string dockerResourceFilePath);
}
