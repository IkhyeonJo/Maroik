namespace Maroik.Core.Domain.Finances;

/// <summary>The current month's projected spending (<see cref="ExpenditureForecastPolicy"/>) — an estimate, never a record.</summary>
/// <param name="SpentSoFar">What this month's records up to the end of today add up to (actual).</param>
/// <param name="Projected">The projected month-end total: <paramref name="SpentSoFar"/> plus the recent months' average after today.</param>
/// <param name="BasisMonths">How many recent months the projection averaged (each had at least one record).</param>
/// <param name="TypicalError">
/// The mean absolute error of the same projection made for the earlier months at the same day of the month (an earlier-month check), or null
/// below <see cref="ExpenditureForecastPolicy.MinimumErrorMonths"/> such months.
/// </param>
/// <param name="ErrorMonths">How many earlier months the typical error was measured on.</param>
public sealed record ExpenditureForecast(decimal SpentSoFar, decimal Projected, int BasisMonths, decimal? TypicalError, int ErrorMonths);
