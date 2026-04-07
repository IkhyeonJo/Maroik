using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IOtherCalendarRepository"/>.</summary>
public class OtherCalendarRepository : IOtherCalendarRepository
{
    private readonly ApplicationDbContext _context;

    public OtherCalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetAllOtherCalendarsAsync()
    {
        return (await _context.OtherCalendars.OrderBy(x => x.CalendarId).ToListAsync()).Adapt<List<OtherCalendarResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email)
    {
        return (await _context.OtherCalendars.Where(x => x.AccountEmail == email).OrderBy(x => x.CalendarId).ToListAsync()).Adapt<List<OtherCalendarResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateOtherCalendarAsync(OtherCalendarRequest request)
    {
        _ = await _context.OtherCalendars.AddAsync(request.Adapt<OtherCalendar>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteOtherCalendarAsync(OtherCalendarRequest request)
    {
        _ = _context.OtherCalendars.Remove(request.Adapt<OtherCalendar>());
        _ = await _context.SaveChangesAsync();
    }
}
