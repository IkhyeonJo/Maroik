using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The calendar rules the Website needs; each member answers from the Domain.</summary>
public interface ICalendarPolicyService
{
    /// <summary>Whether a date lies in the years a calendar event may use.</summary>
    bool IsWithinCalendarEventYears(DateTime date);

    /// <summary>The reminder limits the calendar pages mirror.</summary>
    CalendarFormRules GetCalendarFormRules();
}
