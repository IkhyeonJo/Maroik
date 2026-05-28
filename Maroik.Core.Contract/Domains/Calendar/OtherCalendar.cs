using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Calendar;

/// <summary>
/// Entity representing a subscription from an account to another user's shared calendar.
/// The composite identity is (<see cref="AccountEmail"/>, <see cref="CalendarId"/>).
/// </summary>
public sealed class OtherCalendar : Entity<(string AccountEmail, long CalendarId)>
{
    /// <summary>Email of the subscriber account.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>ID of the calendar being subscribed to.</summary>
    public long CalendarId { get; private set; }

    private OtherCalendar(Email accountEmail, long calendarId)
        : base((accountEmail.Value, calendarId))
    {
        AccountEmail = accountEmail;
        CalendarId = calendarId;
    }

    /// <summary>
    /// Rebuilds an <see cref="OtherCalendar"/> from trusted raw values from the repository layer.
    /// Bypasses validation ?? use only with data from the trusted repository layer.
    /// </summary>
    public static OtherCalendar Reconstitute(string accountEmail, long calendarId)
        => new(Email.FromTrustedSource(accountEmail), calendarId);

    /// <summary>Creates a new calendar subscription after validating the subscriber email.</summary>
    public static ErrorOr<OtherCalendar> Create(string? accountEmailValue, long calendarId)
    {
        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        if (calendarId <= 0)
            return Error.Validation("OtherCalendar.InvalidCalendarId", "Calendar ID must be a positive integer.");

        return new OtherCalendar(emailResult.Value, calendarId);
    }
}
