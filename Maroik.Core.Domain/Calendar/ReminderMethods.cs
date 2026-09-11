namespace Maroik.Core.Domain.Calendar;

/// <summary>
/// Canonical notification-delivery-method strings stored verbatim in
/// <see cref="CalendarEventReminder.Method"/>. Mirrors the DB's
/// <c>CalendarEventReminder_Method_check</c> constraint (<c>Email</c> / <c>Notification</c> only) —
/// this is the single source of truth for the value across every layer, following the same pattern
/// as <see cref="Maroik.Core.Domain.Board.BoardTypes"/>.
/// </summary>
public static class ReminderMethods
{
    /// <summary>Deliver the reminder by e-mail.</summary>
    public const string Email = "Email";

    /// <summary>Deliver the reminder as an in-app/push notification.</summary>
    public const string Notification = "Notification";

    /// <summary>Every recognised delivery method.</summary>
    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Email, Notification };

    /// <summary>Returns true when <paramref name="method"/> is one of the known delivery methods.</summary>
    public static bool IsKnown(string? method) => method != null && All.Contains(method);
}
