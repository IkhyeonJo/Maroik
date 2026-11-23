using System.Globalization;
using ErrorOr;
using Maroik.Core.Domain.Errors;
using Maroik.Core.Domain.Extensions;

namespace Maroik.Core.Domain.Assistants;

/// <summary>
/// The days a tool of the AI chat reads, both ends included, in the account's time zone. The model only names the period ("this_month")
/// or gives two dates; the server works out the days from the account's today, since a small model gets relative dates wrong.
/// </summary>
/// <param name="StartDate">The first day.</param>
/// <param name="EndDate">The last day.</param>
public sealed record AssistantPeriod(DateOnly StartDate, DateOnly EndDate)
{
    /// <summary>The period names a tool accepts, in the order the tool descriptions list them.</summary>
    public static readonly IReadOnlyList<string> Names =
        ["today", "yesterday", "this_week", "last_week", "this_month", "last_month", "this_year", "last_year", "last_7_days", "last_30_days"];

    /// <summary>The date format of <c>start_date</c> / <c>end_date</c>.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Resolves what the model asked for. Two dates win over a name; a blank name with no dates means this month. A week starts on
    /// Monday. The name is matched without regard to case or surrounding spaces.
    /// </summary>
    /// <param name="period">One of <see cref="Names"/>, or blank.</param>
    /// <param name="startDate">The first day as <see cref="DateFormat"/>, or blank.</param>
    /// <param name="endDate">The last day as <see cref="DateFormat"/>, or blank.</param>
    /// <param name="today">Today in the account's time zone.</param>
    public static ErrorOr<AssistantPeriod> Resolve(string? period, string? startDate, string? endDate, DateOnly today)
    {
        bool hasStart = !string.IsNullOrWhiteSpace(startDate);
        bool hasEnd = !string.IsNullOrWhiteSpace(endDate);
        if (hasStart || hasEnd)
            return FromDates(startDate, endDate);

        string name = string.IsNullOrWhiteSpace(period) ? "this_month" : period.Trim().ToLowerInvariant();
        DateOnly monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        DateOnly firstOfMonth = new(today.Year, today.Month, 1);
        return name switch
        {
            "today" => new AssistantPeriod(today, today),
            "yesterday" => new AssistantPeriod(today.AddDays(-1), today.AddDays(-1)),
            "this_week" => new AssistantPeriod(monday, monday.AddDays(6)),
            "last_week" => new AssistantPeriod(monday.AddDays(-7), monday.AddDays(-1)),
            "this_month" => new AssistantPeriod(firstOfMonth, firstOfMonth.AddMonths(1).AddDays(-1)),
            "last_month" => new AssistantPeriod(firstOfMonth.AddMonths(-1), firstOfMonth.AddDays(-1)),
            "this_year" => new AssistantPeriod(new DateOnly(today.Year, 1, 1), new DateOnly(today.Year, 12, 31)),
            "last_year" => new AssistantPeriod(new DateOnly(today.Year - 1, 1, 1), new DateOnly(today.Year - 1, 12, 31)),
            "last_7_days" => new AssistantPeriod(today.AddDays(-6), today),
            "last_30_days" => new AssistantPeriod(today.AddDays(-29), today),
            _ => DomainError.Validation("Assistant.UnknownPeriod", "Unknown period. Use one of: {0}.", string.Join(", ", Names)),
        };
    }

    /// <summary>The period of two explicit dates, refused when one is missing or malformed, out of order, or too long.</summary>
    private static ErrorOr<AssistantPeriod> FromDates(string? startDate, string? endDate)
    {
        if (!TryParseDate(startDate, out DateOnly start) || !TryParseDate(endDate, out DateOnly end))
            return DomainError.Validation("Assistant.InvalidDates", "Give both start_date and end_date as {0}.", DateFormat);
        if (start > end)
            return DomainError.Validation("Assistant.StartAfterEnd", "start_date must not be after end_date.");
        if (end.DayNumber - start.DayNumber + 1 > AssistantPolicy.MaxPeriodDays)
            return DomainError.Validation("Assistant.PeriodTooLong", "A period can be at most {0} days long.", AssistantPolicy.MaxPeriodDays);
        return new AssistantPeriod(start, end);
    }

    /// <summary>Parses one <see cref="DateFormat"/> date.</summary>
    private static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(text?.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// The period as UTC bounds for the stored records: from local midnight of <see cref="StartDate"/> up to, not including, local
    /// midnight after <see cref="EndDate"/>.
    /// </summary>
    /// <param name="timeZoneIanaId">The account's time zone.</param>
    public (DateTime StartUtc, DateTime EndUtcExclusive) ToUtcRange(string timeZoneIanaId) =>
        (StartDate.ToDateTime(TimeOnly.MinValue).ConvertToUtcByTimeZoneIanaId(timeZoneIanaId),
         EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue).ConvertToUtcByTimeZoneIanaId(timeZoneIanaId));
}
