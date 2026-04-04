namespace Maroik.WebSite.Models.ViewModels.Calendar;

public class CalendarOutputViewModel
{
    public IEnumerable<Maroik.Core.Contract.Dtos.CalendarResponse> Calendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> CalendarEventOutputViewModels { get; set; } = [];
    public IEnumerable<Maroik.Core.Contract.Dtos.CalendarResponse> OtherCalendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> OtherCalendarEventOutputViewModels { get; set; } = [];
}