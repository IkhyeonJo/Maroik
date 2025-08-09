using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services;

public class CalendarRepository(ApplicationDbContext context) : ICalendarRepository
{
    /// <summary>
    /// 모든 달력을 구합니다.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Calendar>> GetAllCalendarsAsync()
    {
        return await context.Calendars.OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>
    /// 로그인 계정에 해당하는 모든 달력을 구합니다.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Calendar>> GetCalendarsAsync(string email)
    {
        return await context.Calendars.Where(x => x.AccountEmail == email).OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>
    /// 달력을 생성합니다.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task CreateCalendarAsync(Calendar calendar)
    {
        _ = await context.Calendars.AddAsync(calendar);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// 달력을 업데이트합니다.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task UpdateCalendarAsync(Calendar calendar)
    {
        _ = context.Calendars.Update(calendar);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// 달력을 삭제합니다.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task DeleteCalendarAsync(Calendar calendar)
    {
        _ = context.Calendars.Remove(calendar);
        _ = await context.SaveChangesAsync();
    }
}