using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CalendarEventReminderRepository : ICalendarEventReminderRepository
    {
        private readonly ApplicationDbContext _context;

        public CalendarEventReminderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 달력 이벤트 알림을 생성합니다.
        /// </summary>
        /// <param name="calendarEventReminder"></param>
        /// <returns></returns>
        public async Task CreateCalendarEventReminderAsync(CalendarEventReminder calendarEventReminder)
        {
            _ = await _context.CalendarReminders.AddAsync(calendarEventReminder);
            _ = await _context.SaveChangesAsync();
        }
    }
}