using Maroik.Core.Contract.Dtos;
using Maroik.Website.Mappings;
using Maroik.Website.Models.ViewModels.Calendar;
using Newtonsoft.Json;

namespace Maroik.Website.Tests.Mappings;

/// <summary>
/// Unit tests for <see cref="CalendarViewModelMapper"/> — UTC/local timezone conversion,
/// the FullCalendar exclusive-end-date grid convention for all-day events, and reminder
/// JSON (de)serialization.
/// </summary>
public class CalendarViewModelMapperTests
{
    private static CalendarEventResponse AllDayEvent(DateTime start, DateTime end) => new()
    {
        Id = 1,
        CalendarId = 1,
        Title = "Trip",
        AllDay = true,
        StartDate = start,
        EndDate = end
    };

    private static CalendarEventResponse TimedEvent(DateTime start, DateTime end, string? startTz = null, string? endTz = null) => new()
    {
        Id = 1,
        CalendarId = 1,
        Title = "Meeting",
        AllDay = false,
        StartDate = start,
        EndDate = end,
        StartDateTimeZoneIanaId = startTz,
        EndDateTimeZoneIanaId = endTz
    };

    // -- ToDisplayViewModel: all-day events -----------------------------------

    [Fact]
    public void ToDisplayViewModel_AllDay_ForGrid_EndDateIsMidnightOfDayAfterLocalEnd()
    {
        var evt = AllDayEvent(
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8");

        Assert.Equal(new DateTime(2025, 6, 10), vm.StartDate);
        Assert.Equal(new DateTime(2025, 6, 13), vm.EndDate); // exclusive end: day after the local end date
    }

    [Fact]
    public void ToDisplayViewModel_AllDay_ForGrid_SetsDisplayStringsAndEmptyTimeZoneLabels()
    {
        var evt = AllDayEvent(
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc));

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8");

        Assert.Equal("2025-06-10", vm.DisplayStartDate);
        Assert.Equal("2025-06-10", vm.DisplayEndDate);
        Assert.Equal("", vm.DisplayStartDateTimeZone);
        Assert.Equal("", vm.DisplayEndDateTimeZone);
        Assert.Equal("UTC", vm.StartDateTimeZoneIanaId);
        Assert.Equal("UTC", vm.EndDateTimeZoneIanaId);
    }

    [Fact]
    public void ToDisplayViewModel_AllDay_NotForGrid_DoesNotSetGridStartEndDate()
    {
        var evt = AllDayEvent(
            new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 12, 0, 0, 0, DateTimeKind.Utc));

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8", forGrid: false);

        Assert.Equal(default, vm.StartDate);
        Assert.Equal(default, vm.EndDate);
        Assert.Equal("2025-06-10", vm.DisplayStartDate);
    }

    // -- ToDisplayViewModel: timed events --------------------------------------

    [Fact]
    public void ToDisplayViewModel_Timed_ForGrid_EndDateIsMidnightOfDayAfterLocalEnd()
    {
        var evt = TimedEvent(
            new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 17, 30, 0, DateTimeKind.Utc),
            "UTC", "UTC");

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8");

        Assert.Equal(new DateTime(2025, 6, 10, 9, 0, 0), vm.StartDate);
        Assert.Equal(new DateTime(2025, 6, 11, 0, 0, 0), vm.EndDate);
        Assert.Equal("2025-06-10 09:00", vm.DisplayStartDate);
        Assert.Equal("2025-06-10 17:30", vm.DisplayEndDate);
    }

    [Fact]
    public void ToDisplayViewModel_Timed_NotForGrid_ReturnsBeforeSettingGridDates()
    {
        var evt = TimedEvent(
            new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 17, 30, 0, DateTimeKind.Utc),
            "UTC", "UTC");

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8", forGrid: false);

        Assert.Equal(default, vm.StartDate);
        Assert.Equal(default, vm.EndDate);
    }

    [Fact]
    public void ToDisplayViewModel_Timed_MissingEventTimeZone_FallsBackToViewerTimeZone()
    {
        var evt = TimedEvent(
            new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 17, 0, 0, DateTimeKind.Utc),
            startTz: null, endTz: null);

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8");

        Assert.Equal("UTC", vm.StartDateTimeZoneIanaId);
        Assert.Equal("UTC", vm.EndDateTimeZoneIanaId);
    }

    [Fact]
    public void ToDisplayViewModel_UnrecognizedTimeZone_DisplayTimeZoneFallsBackToRawIanaId()
    {
        var evt = TimedEvent(
            new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 17, 0, 0, DateTimeKind.Utc),
            "Not/ARealZone", "Not/ARealZone");

        var vm = evt.ToDisplayViewModel("UTC", "#3788d8");

        Assert.Equal("Not/ARealZone", vm.DisplayStartDateTimeZone);
        Assert.Equal("Not/ARealZone", vm.DisplayEndDateTimeZone);
    }

    [Fact]
    public void ToDisplayViewModel_CopiesIdAndColorAndCalendarType()
    {
        var evt = TimedEvent(
            new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 6, 10, 17, 0, 0, DateTimeKind.Utc),
            "UTC", "UTC");
        evt.Id = 42;
        evt.CalendarId = 7;

        var vm = evt.ToDisplayViewModel("UTC", "#abcdef", calendarType: "Other");

        Assert.Equal(42, vm.Id);
        Assert.Equal(7, vm.CalendarId);
        Assert.Equal("#abcdef", vm.HtmlColorCode);
        Assert.Equal("Other", vm.CalendarType);
    }

    // -- ToSerializedReminders --------------------------------------------------

    [Fact]
    public void ToSerializedReminders_SerializesAllFields()
    {
        List<CalendarEventReminderDto> reminders =
        [
            new CalendarEventReminderDto
            {
                Method = "Email",
                MinutesBeforeEvent = 30,
                HoursBeforeEvent = 1,
                DaysBeforeEvent = 2,
                WeeksBeforeEvent = 1,
                TimesBeforeEvent = new TimeOnly(9, 0)
            }
        ];

        string json = reminders.ToSerializedReminders();
        var roundTripped = JsonConvert.DeserializeObject<List<CalendarReminderDto>>(json)!;

        Assert.Equal("Email", roundTripped[0].Method);
        Assert.Equal(30, roundTripped[0].MinutesBeforeEvent);
        Assert.Equal(1, roundTripped[0].HoursBeforeEvent);
        Assert.Equal(2, roundTripped[0].DaysBeforeEvent);
        Assert.Equal(1, roundTripped[0].WeeksBeforeEvent);
        Assert.Equal(new TimeSpan(9, 0, 0), roundTripped[0].TimesBeforeEvent);
    }

    [Fact]
    public void ToSerializedReminders_NullMethod_DefaultsToEmptyString()
    {
        List<CalendarEventReminderDto> reminders = [new CalendarEventReminderDto { Method = null }];

        string json = reminders.ToSerializedReminders();
        var roundTripped = JsonConvert.DeserializeObject<List<CalendarReminderDto>>(json)!;

        Assert.Equal("", roundTripped[0].Method);
    }

    // -- PopulateTimeData ---------------------------------------------------------

    [Fact]
    public void PopulateTimeData_GeneratesA24HourGridInFifteenMinuteIncrements()
    {
        var vm = new CalendarOutputViewModel();

        vm.PopulateTimeData("UTC");

        Assert.Equal(96, vm.TimeIntervals.Count); // 24h * 4 (15-min slots)
        Assert.Equal("00:00", vm.TimeIntervals[0]);
        Assert.Equal("23:45", vm.TimeIntervals[^1]);
    }

    [Fact]
    public void PopulateTimeData_CurrentIntervalIsConsistentWithCurrentHourAndMinute()
    {
        var vm = new CalendarOutputViewModel();

        vm.PopulateTimeData("UTC");

        int roundedMinute = (int)Math.Round(vm.CurrentMinute / 15.0) * 15;
        int expectedHour = vm.CurrentHour;
        if (roundedMinute == 60) { expectedHour = (expectedHour + 1) % 24; roundedMinute = 0; }
        string expectedInterval = $"{expectedHour:D2}:{roundedMinute:D2}";

        Assert.Equal(expectedInterval, vm.CurrentInterval);
    }

    // -- ToCalendarEventRequest: all-day ------------------------------------------

    [Fact]
    public void ToCalendarEventRequest_AllDay_ParsesDatesAsUtcMidnightAndClearsTimeZones()
    {
        var vm = new CalendarEventInputViewModel
        {
            AllDay = true,
            StartDate = "2025-06-10",
            EndDate = "2025-06-12",
            StartDateTimeZoneIanaId = "Asia/Seoul",
            EndDateTimeZoneIanaId = "Asia/Seoul"
        };

        var req = vm.ToCalendarEventRequest();

        Assert.Equal(new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc), req.StartDate);
        Assert.Equal(new DateTime(2025, 6, 12, 0, 0, 0, DateTimeKind.Utc), req.EndDate);
        Assert.Null(req.StartDateTimeZoneIanaId);
        Assert.Null(req.EndDateTimeZoneIanaId);
    }

    // -- ToCalendarEventRequest: timed --------------------------------------------

    [Fact]
    public void ToCalendarEventRequest_Timed_ConvertsLocalDateTimeToUtcUsingSuppliedTimeZone()
    {
        var vm = new CalendarEventInputViewModel
        {
            AllDay = false,
            StartDate = "2025-06-10 09:00",
            EndDate = "2025-06-10 17:00",
            StartDateTimeZoneIanaId = "UTC",
            EndDateTimeZoneIanaId = "UTC"
        };

        var req = vm.ToCalendarEventRequest();

        Assert.Equal(new DateTime(2025, 6, 10, 9, 0, 0, DateTimeKind.Utc), req.StartDate);
        Assert.Equal(new DateTime(2025, 6, 10, 17, 0, 0, DateTimeKind.Utc), req.EndDate);
        Assert.Equal("UTC", req.StartDateTimeZoneIanaId);
    }

    [Fact]
    public void ToCalendarEventRequest_Timed_UnrecognizedTimeZone_ReturnsParsedLocalValueUnchanged()
    {
        var vm = new CalendarEventInputViewModel
        {
            AllDay = false,
            StartDate = "2025-06-10 09:00",
            EndDate = "2025-06-10 17:00",
            StartDateTimeZoneIanaId = "Not/ARealZone",
            EndDateTimeZoneIanaId = "Not/ARealZone"
        };

        var req = vm.ToCalendarEventRequest();

        Assert.Equal(new DateTime(2025, 6, 10, 9, 0, 0), req.StartDate);
    }

    [Fact]
    public void ToCalendarEventRequest_CopiesTitleDescriptionLocationStatus()
    {
        var vm = new CalendarEventInputViewModel
        {
            Id = 5,
            CalendarId = 2,
            AllDay = true,
            StartDate = "2025-06-10",
            EndDate = "2025-06-10",
            Title = "Trip",
            Description = "Beach",
            Location = "Busan",
            Status = "Confirmed"
        };

        var req = vm.ToCalendarEventRequest();

        Assert.Equal(5, req.Id);
        Assert.Equal(2, req.CalendarId);
        Assert.Equal("Trip", req.Title);
        Assert.Equal("Beach", req.Description);
        Assert.Equal("Busan", req.Location);
        Assert.Equal("Confirmed", req.Status);
    }

    // -- ToReminderInfoList --------------------------------------------------------

    [Fact]
    public void ToReminderInfoList_NullOrEmpty_ReturnsEmptyList()
    {
        Assert.Empty(((string?)null).ToReminderInfoList());
        Assert.Empty("".ToReminderInfoList());
    }

    [Fact]
    public void ToReminderInfoList_ParsesFieldsAndTimeOfDay()
    {
        string json = JsonConvert.SerializeObject(new[]
        {
            new
            {
                Method = "Push",
                MinutesBeforeEvent = 15,
                HoursBeforeEvent = 0,
                DaysBeforeEvent = 1,
                WeeksBeforeEvent = 0,
                TimesBeforeEvent = "09:30"
            }
        });

        var reminders = json.ToReminderInfoList();

        Assert.Single(reminders);
        Assert.Equal("Push", reminders[0].Method);
        Assert.Equal(15, reminders[0].MinutesBeforeEvent);
        Assert.Equal(1, reminders[0].DaysBeforeEvent);
        Assert.Equal(new TimeSpan(9, 30, 0), reminders[0].TimesBeforeEvent);
    }

    [Fact]
    public void ToReminderInfoList_EmptyTimesBeforeEvent_LeavesTimesBeforeEventNull()
    {
        string json = JsonConvert.SerializeObject(new[]
        {
            new
            {
                Method = "Push",
                MinutesBeforeEvent = 15,
                HoursBeforeEvent = 0,
                DaysBeforeEvent = 0,
                WeeksBeforeEvent = 0,
                TimesBeforeEvent = ""
            }
        });

        var reminders = json.ToReminderInfoList();

        Assert.Null(reminders[0].TimesBeforeEvent);
    }
}
