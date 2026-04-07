using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarEventRepository"/>.</summary>
public class CalendarEventRepository : ICalendarEventRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId)
    {
        return (await _context.CalendarEvents.Where(x => x.CalendarId == calendarId).ToListAsync()).Adapt<List<CalendarEventResponse>>();
    }

    /// <inheritdoc />
    public async Task<CalendarEventResponse?> GetCalendarEventAsync(long id)
    {
        CalendarEvent? calendarEvent = await _context.CalendarEvents.Where(x => x.Id == id).FirstOrDefaultAsync();
        return calendarEvent?.Adapt<CalendarEventResponse>();
    }

    /// <inheritdoc />
    public async Task CreateCalendarEventAsync(CalendarEventRequest request)
    {
        _ = await _context.CalendarEvents.AddAsync(request.Adapt<CalendarEvent>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarEventAsync(CalendarEventRequest request)
    {
        _ = _context.CalendarEvents.Update(request.Adapt<CalendarEvent>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCalendarEventAsync(CalendarEventRequest request)
    {
        _ = _context.CalendarEvents.Remove(request.Adapt<CalendarEvent>());
        _ = await _context.SaveChangesAsync();
    }
}
