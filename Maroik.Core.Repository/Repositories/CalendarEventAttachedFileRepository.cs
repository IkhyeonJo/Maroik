using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventAttachedFileRepository : ICalendarEventAttachedFileRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventAttachedFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CalendarEventAttachedFileResponse>> GetAllAsync()
    {
        return (await _context.CalendarEventAttachedFiles.ToListAsync()).Adapt<List<CalendarEventAttachedFileResponse>>();
    }

    public async Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId)
    {
        CalendarEventAttachedFile? file = await _context.CalendarEventAttachedFiles
            .Where(x => x.CalendarEventId == calendarEventId).FirstOrDefaultAsync();
        return file?.Adapt<CalendarEventAttachedFileResponse>();
    }

    public async Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request)
    {
        _ = await _context.CalendarEventAttachedFiles.AddAsync(request.Adapt<CalendarEventAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request)
    {
        _ = _context.CalendarEventAttachedFiles.Update(request.Adapt<CalendarEventAttachedFile>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""DELETE FROM "CalendarEventAttachedFile" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}
