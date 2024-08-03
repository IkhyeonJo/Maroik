using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface ICalendarEventRepository
    {
        /// <summary>
        /// 해당 달력에 해당하는 모든 달력 이벤트를 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(int calendarId);

        /// <summary>
        /// 한 달력 이벤트를 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<CalendarEvent> GetCalendarEventAsync(int id);

        /// <summary>
        /// 달력 이벤트를 생성합니다.
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public Task CreateCalendarEventAsync(CalendarEvent calendarEvent);

        /// <summary>
        /// 달력 이벤트를 삭제합니다.
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public Task DeleteCalendarEventAsync(CalendarEvent calendarEvent);
    }
}