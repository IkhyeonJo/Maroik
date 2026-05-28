using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarShared table.
/// Lookup/create operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ICalendarSharedRepository : IGenericRepository<CalendarShared>
{
}
