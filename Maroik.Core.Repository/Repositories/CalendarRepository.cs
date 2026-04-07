using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarRepository"/>.</summary>
public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetAllCalendarsAsync()
    {
        return (await _context.Calendars.OrderBy(x => x.Name).ToListAsync()).Adapt<List<CalendarResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<CalendarResponse>> GetCalendarsAsync(string email)
    {
        return (await _context.Calendars.Where(x => x.AccountEmail == email).OrderBy(x => x.Name).ToListAsync()).Adapt<List<CalendarResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateCalendarAsync(CalendarRequest request)
    {
        _ = await _context.Calendars.AddAsync(request.Adapt<Calendar>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarAsync(CalendarRequest request)
    {
        _ = _context.Calendars.Update(request.Adapt<Calendar>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCalendarAsync(CalendarRequest request)
    {
        _ = _context.Calendars.Remove(request.Adapt<Calendar>());
        _ = await _context.SaveChangesAsync();
    }
}
