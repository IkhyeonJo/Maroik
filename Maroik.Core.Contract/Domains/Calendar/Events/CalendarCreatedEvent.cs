using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Domains.ValueObjects;

namespace Maroik.Core.Contract.Domains.Calendar.Events;

/// <summary>
/// Raised when a new calendar is created by an account owner.
/// </summary>
public sealed record CalendarCreatedEvent(
    long CalendarId,
    Email AccountEmail,
    string CalendarName) : DomainEvent;
