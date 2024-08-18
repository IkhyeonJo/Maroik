using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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
        /// 해당 달력 이벤트에 해당하는 모든 달력 이벤트 알림을 구합니다.
        /// </summary>
        /// <param name="calendarEventId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<CalendarEventReminder>> GetCalendarEventRemindersAsync(int calendarEventId)
        {
            return await _context.CalendarReminders.Where(x => x.CalendarEventId == calendarEventId).ToListAsync();
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

        /// <summary>
        /// 달력 이벤트 알림들을 삭제합니다.
        /// </summary>
        /// <param name="calendarEventId"></param>
        /// <returns></returns>
        public async Task<int> DeleteCalendarEventReminderAsync(int calendarEventId)
        {
            return await _context.Database.ExecuteSqlInterpolatedAsync
            (
                $"DELETE FROM CalendarEventReminder WHERE CalendarEventId = {calendarEventId}"
            );
        }
    }
}