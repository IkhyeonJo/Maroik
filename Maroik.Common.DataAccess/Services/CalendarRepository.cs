using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CalendarRepository : ICalendarRepository
    {
        private readonly ApplicationDbContext _context;

        public CalendarRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 로그인 계정에 해당하는 모든 달력을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<List<Calendar>> GetCalendarsAsync(string email)
        {
            return await _context.Calendars.Where(x => x.AccountEmail == email).ToListAsync();
        }

        /// <summary>
        /// 달력을 생성합니다.
        /// </summary>
        /// <param name="calendar"></param>
        /// <returns></returns>
        public async Task CreateCalendarAsync(Calendar calendar)
        {
            _ = await _context.Calendars.AddAsync(calendar);
            _ = await _context.SaveChangesAsync();
        }
    }
}