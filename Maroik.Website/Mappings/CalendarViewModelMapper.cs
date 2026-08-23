using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Website.Models.ViewModels.Calendar;
using Newtonsoft.Json;

namespace Maroik.Website.Mappings;

/// <summary>
/// Maps between calendar DTOs and view models in both directions:
/// Response → OutputViewModel (display) and InputViewModel → Request (form submission).
/// Centralizes UTC↔local timezone conversion and reminder serialization/deserialization.
/// </summary>
public static class CalendarViewModelMapper
{
    extension(CalendarEventResponse calendarEvent)
    {
        /// <summary>
        /// Maps a calendar event DTO to a <see cref="CalendarEventOutputViewModel"/>.
        /// Converts UTC dates to the viewer's local timezone and populates all display strings.
        /// </summary>
        /// <param name="userTimeZoneIanaId">Viewer's IANA timezone ID (used for all-day events and as fallback).</param>
        /// <param name="htmlColorCode">Parent calendar color code for chip/badge styling.</param>
        /// <param name="calendarType">Optional "My" / "Other" label; omit for single-owner views.</param>
        /// <param name="forGrid">
        /// When <see langword="true"/> (default), sets <c>StartDate</c> / <c>EndDate</c> with the
        /// FullCalendar-compatible exclusive-end convention (EndDate = midnight of the day after local end).
        /// Pass <see langword="false"/> for detail / edit views that only need the display strings.
        /// </param>
        public CalendarEventOutputViewModel ToDisplayViewModel(
            string userTimeZoneIanaId,
            string? htmlColorCode,
            string? calendarType = null,
            bool forGrid = true)
        {
            CalendarEventOutputViewModel item = new()
            {
                Id = calendarEvent.Id,
                CalendarId = calendarEvent.CalendarId,
                Title = calendarEvent.Title,
                AllDay = calendarEvent.AllDay,
                HtmlColorCode = htmlColorCode,
                CalendarType = calendarType
            };

            if (calendarEvent.AllDay)
            {
                DateTime localStart = calendarEvent.StartDate.ConvertTimeByTimeZoneIanaId(userTimeZoneIanaId);
                DateTime localEnd = calendarEvent.EndDate.ConvertTimeByTimeZoneIanaId(userTimeZoneIanaId);

                item.StartDateTimeZoneIanaId = userTimeZoneIanaId;
                item.EndDateTimeZoneIanaId = userTimeZoneIanaId;
                item.DisplayStartDate = localStart.ToString("yyyy-MM-dd");
                item.DisplayEndDate = localEnd.ToString("yyyy-MM-dd");
                item.DisplayStartDateTimeZone = "";
                item.DisplayEndDateTimeZone = "";

                if (!forGrid)
                {
                    return item;
                }

                item.StartDate = localStart;
                DateTime nextDay = localEnd.AddDays(1);
                item.EndDate = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0, nextDay.Kind);
            }
            else
            {
                string startTzId = calendarEvent.StartDateTimeZoneIanaId ?? userTimeZoneIanaId;
                string endTzId = calendarEvent.EndDateTimeZoneIanaId ?? userTimeZoneIanaId;

                DateTime localStart = calendarEvent.StartDate.ConvertTimeByTimeZoneIanaId(startTzId);
                DateTime localEnd = calendarEvent.EndDate.ConvertTimeByTimeZoneIanaId(endTzId);

                item.StartDateTimeZoneIanaId = startTzId;
                item.EndDateTimeZoneIanaId = endTzId;
                item.DisplayStartDate = localStart.ToString("yyyy-MM-dd HH:mm");
                item.DisplayEndDate = localEnd.ToString("yyyy-MM-dd HH:mm");
                item.DisplayStartDateTimeZone = GetStandardName(startTzId);
                item.DisplayEndDateTimeZone = GetStandardName(endTzId);

                if (!forGrid)
                {
                    return item;
                }

                item.StartDate = localStart;
                DateTime nextDay = localEnd.AddDays(1);
                item.EndDate = new DateTime(nextDay.Year, nextDay.Month, nextDay.Day, 0, 0, 0, nextDay.Kind);
            }

            return item;
        }
    }

    extension(IEnumerable<CalendarEventReminderDto> reminders)
    {
        /// <summary>
        /// Serializes reminder response objects to the JSON array the client edit-form expects.
        /// </summary>
        public string ToSerializedReminders()
            => JsonConvert.SerializeObject(reminders.Select(x => new CalendarReminderDto
            {
                Method = x.Method ?? "",
                MinutesBeforeEvent = (int?)x.MinutesBeforeEvent,
                HoursBeforeEvent = (int?)x.HoursBeforeEvent,
                DaysBeforeEvent = (int?)x.DaysBeforeEvent,
                WeeksBeforeEvent = (int?)x.WeeksBeforeEvent,
                TimesBeforeEvent = x.TimesBeforeEvent?.ToTimeSpan()
            }));
    }

    extension(CalendarOutputViewModel vm)
    {
        /// <summary>
        /// Fills the time-grid fields on the view model for the viewer's current local time.
        /// </summary>
        /// <exception cref="FormatException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void PopulateTimeData(string timeZoneIanaId)
        {
            DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId);
            vm.CurrentDate = ct.ToString("yyyy-MM-dd");
            vm.CurrentHour = ct.Hour;
            vm.CurrentMinute = ct.Minute;
            vm.CurrentSecond = ct.Second;
            List<string> ti = [];
            for (int h = 0; h < 24; h++)
                for (int m = 0; m < 60; m += 15)
                    ti.Add($"{h:D2}:{m:D2}");
            vm.TimeIntervals = ti;
            int nm = (int)Math.Round(ct.Minute / 15.0) * 15;
            if (nm == 60) { ct = ct.AddHours(1); nm = 0; }
            vm.CurrentInterval = $"{ct.Hour:D2}:{nm:D2}";
        }
    }

    extension(CalendarEventInputViewModel vm)
    {
        /// <summary>
        /// Builds a <see cref="CalendarEventRequest"/> from the posted form view model.
        /// All-day events are stored as UTC midnight; timed events are converted from the
        /// user-supplied IANA timezone to UTC.
        /// </summary>
        public CalendarEventRequest ToCalendarEventRequest()
        {
            CalendarEventRequest req = new() { Id = vm.Id };

            if (vm.AllDay)
            {
                string[] d = vm.StartDate!.Split('-');
                req.StartDate = new DateTime(int.Parse(d[0]), int.Parse(d[1]), int.Parse(d[2]), 0, 0, 0, DateTimeKind.Utc);
                d = vm.EndDate!.Split('-');
                req.EndDate = new DateTime(int.Parse(d[0]), int.Parse(d[1]), int.Parse(d[2]), 0, 0, 0, DateTimeKind.Utc);
                req.StartDateTimeZoneIanaId = null;
                req.EndDateTimeZoneIanaId = null;
            }
            else
            {
                req.StartDate = ParseDateTimeToUtc(vm.StartDate!, vm.StartDateTimeZoneIanaId!);
                req.EndDate = ParseDateTimeToUtc(vm.EndDate!, vm.EndDateTimeZoneIanaId!);
                req.StartDateTimeZoneIanaId = vm.StartDateTimeZoneIanaId;
                req.EndDateTimeZoneIanaId = vm.EndDateTimeZoneIanaId;
            }

            req.CalendarId = vm.CalendarId;
            req.Title = vm.Title;
            req.Description = vm.Description;
            req.AllDay = vm.AllDay;
            req.Location = vm.Location;
            req.Status = vm.Status;
            return req;
        }
    }

    extension(string? serialized)
    {
        /// <summary>
        /// Deserializes the JSON reminder list from the form submission into a strongly-typed list.
        /// Returns an empty list when the source string is null or empty.
        /// </summary>
        public List<CalendarReminderDto> ToReminderInfoList()
        {
            List<CalendarReminderDto> reminders = [];
            List<Dictionary<string, object>> raw = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(serialized ?? "[]") ?? [];
            reminders.AddRange(from r in raw
                let tempTimes = r["TimesBeforeEvent"].ToString()
                select new CalendarReminderDto
                {
                    Method = r["Method"].ToString() ?? "",
                    MinutesBeforeEvent = r["MinutesBeforeEvent"] != null ? Convert.ToInt32(r["MinutesBeforeEvent"]) : null,
                    HoursBeforeEvent = r["HoursBeforeEvent"] != null ? Convert.ToInt32(r["HoursBeforeEvent"]) : null,
                    DaysBeforeEvent = r["DaysBeforeEvent"] != null ? Convert.ToInt32(r["DaysBeforeEvent"]) : null,
                    WeeksBeforeEvent = r["WeeksBeforeEvent"] != null ? Convert.ToInt32(r["WeeksBeforeEvent"]) : null,
                    TimesBeforeEvent = !string.IsNullOrEmpty(tempTimes)
                        ? new TimeSpan(Convert.ToInt32(tempTimes.Split(':')[0]), Convert.ToInt32(tempTimes.Split(':')[1]), 0)
                        : null
                });
            return reminders;
        }
    }

    /// <summary>
    /// Returns the <see cref="TimeZoneInfo.StandardName"/> for an IANA timezone ID by scanning
    /// system timezones (cross-platform Windows/IANA ID mapping included).
    /// Falls back to the raw IANA ID string if no match is found.
    /// </summary>
    private static string GetStandardName(string ianaId)
    {
        foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
        {
            string tzIanaId = tz.HasIanaId
                ? tz.Id
                : (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string? id) ? id : "");

            if (!string.IsNullOrEmpty(tzIanaId) && tzIanaId == ianaId)
                return tz.StandardName;
        }
        return ianaId;
    }

    /// <summary>
    /// Parses a "yyyy-MM-dd HH:mm" local datetime string and converts it to UTC
    /// using the supplied IANA timezone ID.
    /// Returns the parsed local value unchanged if the timezone is not found.
    /// </summary>
    private static DateTime ParseDateTimeToUtc(string dateTimeStr, string ianaId)
    {
        string[] parts = dateTimeStr.Split(' ');
        string[] dateParts = parts[0].Split('-');
        string[] timeParts = parts[1].Split(':');
        DateTime local = new(
            int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]),
            int.Parse(timeParts[0]), int.Parse(timeParts[1]), 0, DateTimeKind.Unspecified);

        foreach (TimeZoneInfo tz in TimeZoneInfo.GetSystemTimeZones())
        {
            string tzId = tz.HasIanaId ? tz.Id :
                (TimeZoneInfo.TryConvertWindowsIdToIanaId(tz.Id, out string? id) ? id : "");
            if (!string.IsNullOrEmpty(tzId) && tzId == ianaId)
                return TimeZoneInfo.ConvertTime(local, tz, TimeZoneInfo.Utc);
        }
        return local;
    }
}
