using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="ICalendarPolicyService"/>
public sealed class CalendarPolicyService : ICalendarPolicyService
{
    /// <inheritdoc />
    public bool IsWithinCalendarEventYears(DateTime date) => CalendarEvent.IsWithinYears(date);

    /// <inheritdoc />
    public CalendarFormRules GetCalendarFormRules() => new()
    {
        MinLeadTimeBeforeEvent = CalendarReminderPolicy.MinLeadTimeBeforeEvent,
        MaxMinutesBeforeEvent = CalendarReminderPolicy.MaxMinutesBeforeEvent,
        MaxHoursBeforeEvent = CalendarReminderPolicy.MaxHoursBeforeEvent,
        MaxDaysBeforeEvent = CalendarReminderPolicy.MaxDaysBeforeEvent,
        MaxWeeksBeforeEvent = CalendarReminderPolicy.MaxWeeksBeforeEvent,
        ReminderTimeOfDayStepMinutes = CalendarReminderPolicy.ReminderTimeOfDayStepMinutes,
        DefaultReminderTimeOfDay = CalendarReminderPolicy.DefaultReminderTimeOfDay,
        MaxRemindersPerEvent = CalendarReminderPolicy.MaxRemindersPerEvent,
    };
}
