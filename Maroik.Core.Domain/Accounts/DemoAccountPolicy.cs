using System.Globalization;

namespace Maroik.Core.Domain.Accounts;

/// <summary>
/// The public demo account: its pages always open on one fixed, pre-seeded period, so a visitor sees the sample data whenever they browse.
/// The account and the period are configuration; a period that does not parse pins nothing.
/// </summary>
public static class DemoAccountPolicy
{
    /// <summary>Whether <paramref name="accountEmail"/> is the configured demo account; with none configured, nobody is.</summary>
    public static bool IsDemoAccount(string? demoAccountEmail, string? accountEmail) =>
        !string.IsNullOrEmpty(demoAccountEmail) && accountEmail == demoAccountEmail;

    /// <summary>The first day of the configured period, or null when it is not a real month.</summary>
    public static DateOnly? FirstDayOf(string? year, string? month) =>
        DateOnly.TryParseExact($"{year}-{month}", "yyyy-M", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly firstDay) ? firstDay : null;

    /// <summary>The configured year, or null when it is not a real year.</summary>
    public static int? YearOf(string? year) =>
        int.TryParse(year, NumberStyles.None, CultureInfo.InvariantCulture, out int value) && value >= DateOnly.MinValue.Year && value <= DateOnly.MaxValue.Year
            ? value
            : null;
}
