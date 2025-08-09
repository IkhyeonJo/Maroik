using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services;

public class CalendarEventAttachedFileRepository(ApplicationDbContext context)
    : ICalendarEventAttachedFileRepository
{
    /// <summary>
    /// 한 첨부파일을 구합니다.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<CalendarEventAttachedFile> GetCalendarEventAttachedFileAsync(long calendarEventId)
    {
        return await context.CalendarEventAttachedFiles.Where(x => x.CalendarEventId == calendarEventId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// 첨부파일을 저장합니다.
    /// </summary>
    /// <param name="calendarEventAttachedFile"></param>
    /// <returns></returns>
    public async Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile)
    {
        _ = await context.CalendarEventAttachedFiles.AddAsync(calendarEventAttachedFile);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// 첨부파일들을 삭제합니다.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId)
    {
        return await context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""DELETE FROM "CalendarEventAttachedFile" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}