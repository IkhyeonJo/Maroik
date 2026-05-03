using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Calendar.Events;

/// <summary>
/// Raised when a calendar's sharing permissions are changed (enabled or revoked).
/// </summary>
public sealed record CalendarSharedEvent(
    long CalendarId,
    bool SharedWithUsers,
    bool SharedWithAnonymous) : DomainEvent;
