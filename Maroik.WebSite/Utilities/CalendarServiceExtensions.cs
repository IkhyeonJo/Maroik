using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.WebSite.Models.ViewModels.Calendar;

namespace Maroik.WebSite.Utilities;

/// <summary>
/// Presentation-layer extension methods on <see cref="ICalendarService"/>.
/// Combines service data-fetching with <see cref="CalendarViewModelMapper"/> mapping
/// so controllers need not orchestrate the fetch-then-map loop themselves.
/// </summary>
public static class CalendarServiceExtensions
{
    extension(ICalendarService calendarService)
    {
        /// <summary>
        /// Fetches all events for each calendar in <paramref name="calendars"/> and returns
        /// them as grid-ready view models with timezone conversion applied.
        /// </summary>
        /// <param name="calendars">Calendars whose events should be loaded.</param>
        /// <param name="userTimeZoneIanaId">Viewer's IANA timezone ID for date conversion.</param>
        /// <param name="calendarType">Optional "My" / "Other" label passed through to each item.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task<List<CalendarEventOutputViewModel>> GetCalendarEventViewModelsAsync(
            IEnumerable<CalendarResponse> calendars,
            string userTimeZoneIanaId,
            string? calendarType,
            CancellationToken ct)
        {
            List<CalendarEventOutputViewModel> items = [];
            foreach (CalendarResponse calendar in calendars)
            {
                foreach (CalendarEventResponse evt in await calendarService.GetCalendarEventsAsync(calendar.Id, ct) ?? [])
                    items.Add(evt.ToDisplayViewModel(userTimeZoneIanaId, calendar.HtmlColorCode, calendarType));
            }
            return items;
        }
    }
}
