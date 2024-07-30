using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CalendarEventAttachedFileRepository : ICalendarEventAttachedFileRepository
    {
        private readonly ApplicationDbContext _context;

        public CalendarEventAttachedFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 등록된 모든 계정을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<List<CalendarEventAttachedFile>> GetAllAsync()
        {
            return await _context.CalendarEventAttachedFiles.ToListAsync();
        }

        /// <summary>
        /// 첨부파일을 저장합니다.
        /// </summary>
        /// <param name="calendarEventAttachedFile"></param>
        /// <returns></returns>
        public async Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile)
        {
            _ = await _context.CalendarEventAttachedFiles.AddAsync(calendarEventAttachedFile);
            _ = await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 첨부파일을 업데이트합니다.
        /// </summary>
        /// <param name="calendarEventAttachedFile"></param>
        /// <returns></returns>
        public async Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile)
        {
            _ = _context.CalendarEventAttachedFiles.Update(calendarEventAttachedFile);
            _ = await _context.SaveChangesAsync();
        }
    }
}