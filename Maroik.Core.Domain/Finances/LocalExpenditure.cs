namespace Maroik.Core.Domain.Finances;

/// <summary>
/// One expenditure as the dashboard's facts read it: when it happened in the account's own time zone (so "the 14th" and
/// "Saturday" are the user's), its sub-class and its amount. The service converts and scopes the records (default currency
/// only) before handing them to <see cref="ExpenditureForecastPolicy"/> and <see cref="ExpenditureInsightPolicy"/>.
/// </summary>
/// <param name="LocalTime">When the expenditure happened, in the account's time zone.</param>
/// <param name="SubClass">Its sub-class, as stored.</param>
/// <param name="Amount">Its amount.</param>
public sealed record LocalExpenditure(DateTime LocalTime, string SubClass, decimal Amount);
