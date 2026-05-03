using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Calendar.Events;

/// <summary>
/// Raised when a new event is added to a calendar.
/// Consumers (e.g. reminder scheduler) may use this to register upcoming reminders.
/// </summary>
public sealed record CalendarEventAddedEvent(
    long CalendarId,
    long CalendarEventId,
    string Title,
    DateTime StartDate) : DomainEvent;
