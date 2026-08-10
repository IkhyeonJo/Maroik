using ErrorOr;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Calendar;

/// <summary>
/// Aggregate root for a calendar.
/// Controls calendar metadata, sharing permissions, and subscriber relationships.
/// Calendar events are separate aggregate roots (referenced by CalendarId).
/// </summary>
public sealed class Calendar : AggregateRoot<long>
{
    private readonly List<OtherCalendar> _subscribers = [];

    /// <summary>Email of the account that owns this calendar.</summary>
    public Email AccountEmail { get; private set; }

    /// <summary>Calendar display name (e.g. "Work", "Personal").</summary>
    public string Name { get; private set; }

    /// <summary>Optional description of the calendar's purpose.</summary>
    public string? Description { get; private set; }

    /// <summary>Default IANA time-zone for events in this calendar.</summary>
    public TimeZoneId TimeZone { get; private set; }

    /// <summary>HTML color code used to visually distinguish this calendar (e.g. "#FF5733").</summary>
    public HtmlColorCode ColorCode { get; private set; }

    /// <summary>UTC timestamp when the calendar was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Sharing permissions for this calendar. Null until explicitly configured.</summary>
    public CalendarShared? Shared { get; private set; }

    /// <summary>Read-only view of accounts subscribed to this calendar.</summary>
    public IReadOnlyList<OtherCalendar> Subscribers => _subscribers.AsReadOnly();

    private Calendar(
        long id,
        Email accountEmail,
        string name,
        string? description,
        TimeZoneId timeZone,
        HtmlColorCode colorCode) : base(id)
    {
        AccountEmail = accountEmail;
        Name = name;
        Description = description;
        TimeZone = timeZone;
        ColorCode = colorCode;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds a <see cref="Calendar"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// Does NOT raise domain events.
    /// </summary>
    public static Calendar Reconstitute(
        long id,
        string accountEmail,
        string name,
        string? description,
        string timeZoneIanaId,
        string htmlColorCode,
        DateTime created,
        DateTime updated)
    {
        var calendar = new Calendar(
            id,
            Email.FromTrustedSource(accountEmail),
            name,
            description,
            TimeZoneId.FromTrustedSource(timeZoneIanaId),
            HtmlColorCode.FromTrustedSource(htmlColorCode)) { Created = created, Updated = updated };

        return calendar;
    }

    /// <summary>
    /// Creates a new calendar.
    /// </summary>
    public static ErrorOr<Calendar> Create(
        string? accountEmailValue,
        string? name,
        string? description,
        string? timeZoneValue,
        string? htmlColorCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Calendar.NameEmpty", "Calendar name cannot be empty.");

        var emailResult = Email.Create(accountEmailValue);
        if (emailResult.IsError) return emailResult.Errors;

        var tzResult = TimeZoneId.Create(timeZoneValue);
        if (tzResult.IsError) return tzResult.Errors;

        var colorResult = HtmlColorCode.Create(htmlColorCode);
        if (colorResult.IsError) return colorResult.Errors;

        var calendar = new Calendar(0, emailResult.Value, name, description, tzResult.Value, colorResult.Value);
        return calendar;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Updates the calendar's display fields.</summary>
    public ErrorOr<Success> Update(
        string? name,
        string? description,
        string? timeZoneValue,
        string? htmlColorCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Calendar.NameEmpty", "Calendar name cannot be empty.");

        var tzResult = TimeZoneId.Create(timeZoneValue);
        if (tzResult.IsError) return tzResult.Errors;

        var colorResult = HtmlColorCode.Create(htmlColorCode);
        if (colorResult.IsError) return colorResult.Errors;

        Name = name;
        Description = description;
        TimeZone = tzResult.Value;
        ColorCode = colorResult.Value;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>
    /// Updates the sharing permissions for this calendar.
    /// </summary>
    public void UpdateSharing(bool sharedWithUsers, bool sharedWithAnonymous)
    {
        Shared ??= CalendarShared.CreatePrivate(Id);

        Shared.Update(sharedWithUsers, sharedWithAnonymous);
        Updated = DateTime.UtcNow;
    }

    /// <summary>Adds a subscriber to this calendar (another account subscribing).</summary>
    public ErrorOr<Success> AddSubscriber(OtherCalendar subscription)
    {
        if (subscription.CalendarId != Id)
            return Error.Validation("Calendar.SubscriberMismatch", "Subscription does not reference this calendar.");

        bool alreadySubscribed = _subscribers.Exists(s => s.AccountEmail.Value == subscription.AccountEmail.Value);
        if (alreadySubscribed)
            return Error.Conflict("Calendar.AlreadySubscribed", "This account is already subscribed to the calendar.");

        _subscribers.Add(subscription);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Removes a subscriber by their email address.</summary>
    public ErrorOr<Success> RemoveSubscriber(string accountEmail)
    {
        var subscription = _subscribers.Find(s => s.AccountEmail.Value == accountEmail.ToLowerInvariant());
        if (subscription is null)
            return Error.NotFound("Calendar.SubscriberNotFound", "The specified account is not subscribed to this calendar.");

        _subscribers.Remove(subscription);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
