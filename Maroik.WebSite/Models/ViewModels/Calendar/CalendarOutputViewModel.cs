using Maroik.Core.Contract.Dtos;

namespace Maroik.WebSite.Models.ViewModels.Calendar;

public class CalendarOutputViewModel
{
    public IEnumerable<CalendarResponse> Calendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> CalendarEventOutputViewModels { get; set; } = [];
    public IEnumerable<CalendarResponse> OtherCalendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> OtherCalendarEventOutputViewModels { get; set; } = [];

    // Populated by controller — consumed by view
    public AccountResponse LoginedAccount { get; set; } = new();
    public string LoginedAccountTimeZoneIanaId { get; set; } = "UTC";
    public string CurrentDate { get; set; } = "";
    public List<string> TimeIntervals { get; set; } = [];
    public string CurrentInterval { get; set; } = "";
    public int CurrentHour { get; set; }
    public int CurrentMinute { get; set; }
    public int CurrentSecond { get; set; }
}