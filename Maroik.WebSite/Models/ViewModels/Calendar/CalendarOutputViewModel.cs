namespace Maroik.WebSite.Models.ViewModels.Calendar;

public class CalendarOutputViewModel
{
    public IEnumerable<Maroik.Core.Repository.Models.Calendar> Calendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> CalendarEventOutputViewModels { get; set; } = [];
    public IEnumerable<Maroik.Core.Repository.Models.Calendar> OtherCalendars { get; set; } = [];
    public IEnumerable<CalendarEventOutputViewModel> OtherCalendarEventOutputViewModels { get; set; } = [];
}