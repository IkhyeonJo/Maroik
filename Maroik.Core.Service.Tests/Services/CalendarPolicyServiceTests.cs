using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="CalendarPolicyService"/>: each member answers what the Domain answers.</summary>
public class CalendarPolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly CalendarPolicyService _sut = new();

    /// <summary>The event years stop before DateTime's edges.</summary>
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(9999, false)]
    public void IsWithinCalendarEventYears_FollowsTheAggregate(int year, bool expected) =>
        Assert.Equal(expected, _sut.IsWithinCalendarEventYears(new DateTime(year, 6, 1, 0, 0, 0, DateTimeKind.Unspecified)));

    /// <summary>The calendar rules carry the reminder policy's limits.</summary>
    [Fact]
    public void GetCalendarFormRules_CarriesThePolicy()
    {
        CalendarFormRules rules = _sut.GetCalendarFormRules();

        Assert.Equal(CalendarReminderPolicy.MinLeadTimeBeforeEvent, rules.MinLeadTimeBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxMinutesBeforeEvent, rules.MaxMinutesBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxHoursBeforeEvent, rules.MaxHoursBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxDaysBeforeEvent, rules.MaxDaysBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.MaxWeeksBeforeEvent, rules.MaxWeeksBeforeEvent);
        Assert.Equal(CalendarReminderPolicy.ReminderTimeOfDayStepMinutes, rules.ReminderTimeOfDayStepMinutes);
        Assert.Equal(CalendarReminderPolicy.DefaultReminderTimeOfDay, rules.DefaultReminderTimeOfDay);
        Assert.Equal(CalendarReminderPolicy.MaxRemindersPerEvent, rules.MaxRemindersPerEvent);
    }
}
