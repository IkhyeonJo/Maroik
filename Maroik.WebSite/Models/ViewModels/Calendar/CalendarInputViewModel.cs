namespace Maroik.WebSite.Models.ViewModels.Calendar;

public class CalendarInputViewModel
{
    public IEnumerable<Maroik.Core.PostgreSQL.Models.Calendar> Calendars { get; set; } = [];
}