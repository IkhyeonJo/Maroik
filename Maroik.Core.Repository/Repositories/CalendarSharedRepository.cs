using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarSharedRepository"/>.</summary>
public class CalendarSharedRepository : ICalendarSharedRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="CalendarSharedRepository"/> with <paramref name="context"/>.</summary>
    public CalendarSharedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<CalendarSharedResponse>> GetCalendarSharedAsync()
    {
        return (await _context.CalendarShareds.OrderBy(x => x.CalendarId).ToListAsync()).Adapt<List<CalendarSharedResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateCalendarSharedAsync(CalendarSharedRequest request)
    {
        _ = await _context.CalendarShareds.AddAsync(request.Adapt<CalendarShared>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarSharedAsync(CalendarSharedRequest request)
    {
        _ = _context.CalendarShareds.Update(request.Adapt<CalendarShared>());
        _ = await _context.SaveChangesAsync();
    }
}
