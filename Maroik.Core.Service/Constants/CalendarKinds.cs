using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Constants;

/// <summary>The calendar kinds, for the code outside the Service layer that may not read the Domain; each value is the Domain's own constant.</summary>
public static class CalendarKinds
{
    /// <summary><see cref="CalendarTypes.My"/>.</summary>
    public const string My = CalendarTypes.My;

    /// <summary><see cref="CalendarTypes.Other"/>.</summary>
    public const string Other = CalendarTypes.Other;
}
