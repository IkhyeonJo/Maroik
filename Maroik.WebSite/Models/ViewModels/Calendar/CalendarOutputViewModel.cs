namespace Maroik.WebSite.Models.ViewModels.Calendar
{
    public class CalendarOutputViewModel
    {
        public IEnumerable<Maroik.Common.DataAccess.Models.Calendar> Calendars { get; set; } = [];
        public IEnumerable<CalendarEventOutputViewModel> CalendarEventOutputViewModels { get; set; } = [];
    }
}