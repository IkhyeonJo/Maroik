using Maroik.Common.DataAccess.Models;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface ICalendarEventRepository
    {
        /// <summary>
        /// 달력 이벤트를 생성합니다.
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public Task CreateCalendarEventAsync(CalendarEvent calendarEvent);
    }
}