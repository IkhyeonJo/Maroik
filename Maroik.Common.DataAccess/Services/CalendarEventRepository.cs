using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CalendarEventRepository : ICalendarEventRepository
    {
        private readonly ApplicationDbContext _context;

        public CalendarEventRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 달력 이벤트를 생성합니다.
        /// </summary>
        /// <param name="calendarEvent"></param>
        /// <returns></returns>
        public async Task CreateCalendarEventAsync(CalendarEvent calendarEvent)
        {
            _ = await _context.CalendarEvents.AddAsync(calendarEvent);
            _ = await _context.SaveChangesAsync();
        }
    }
}