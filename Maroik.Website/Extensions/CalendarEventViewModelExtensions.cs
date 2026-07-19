using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Website.Mappings;
using Maroik.Website.Models.ViewModels.Calendar;

namespace Maroik.Website.Extensions;

/// <summary>
/// Presentation-layer extensions on <see cref="ICalendarService"/> that combine
/// service data-fetching with <see cref="CalendarViewModelMapper"/> mapping,
/// so controllers receive ready-to-render view model lists without orchestrating the loop.
/// </summary>
public static class CalendarEventViewModelExtensions
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
                items.AddRange(from evt in await calendarService.GetCalendarEventsAsync(calendar.Id, ct) select evt.ToDisplayViewModel(userTimeZoneIanaId, calendar.HtmlColorCode, calendarType));
            }
            return items;
        }
    }
}
