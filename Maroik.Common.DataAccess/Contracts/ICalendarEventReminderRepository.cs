using Maroik.Common.DataAccess.Models;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface ICalendarEventReminderRepository
    {
        /// <summary>
        /// 달력 이벤트 알림을 생성합니다.
        /// </summary>
        /// <param name="calendarEventReminder"></param>
        /// <returns></returns>
        public Task CreateCalendarEventReminderAsync(CalendarEventReminder calendarEventReminder);
    }
}