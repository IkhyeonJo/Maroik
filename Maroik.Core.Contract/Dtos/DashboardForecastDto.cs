namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// The current month's projected spending in the dashboard's currency (null on <see cref="DashboardDto"/> for any other month): an
/// estimate, shown with what it was computed from and how far the same projection was off before — never a record.
/// </summary>
public class DashboardForecastDto
{
    /// <summary>What this month's records up to the end of today add up to.</summary>
    public decimal SpentSoFar { get; init; }

    /// <summary>The projected month-end total.</summary>
    public decimal Projected { get; init; }

    /// <summary>How many recent months the projection averaged.</summary>
    public int BasisMonths { get; init; }

    /// <summary>
    /// The mean absolute error of the same projection for earlier months at the same day (an earlier-month check), or null when too few earlier
    /// months could be projected.
    /// </summary>
    public decimal? TypicalError { get; init; }

    /// <summary>How many earlier months the typical error was measured on.</summary>
    public int ErrorMonths { get; init; }

    /// <summary>The account's date the projection was made on (its "today").</summary>
    public DateOnly AsOf { get; init; }
}
