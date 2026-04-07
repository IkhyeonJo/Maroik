namespace Maroik.WebSite.Models.ViewModels.Calendar;

/// <summary>
/// View model passed to the Calendar create/edit form.
/// Carries the current user's existing calendars so the form can display
/// a calendar-selector dropdown when creating or editing an event.
/// </summary>
public class CalendarInputViewModel
{
    /// <summary>All calendars owned by the logged-in user; pre-populated by the controller.</summary>
    public IEnumerable<Maroik.Core.Contract.Dtos.CalendarRequest> Calendars { get; set; } = [];
}
