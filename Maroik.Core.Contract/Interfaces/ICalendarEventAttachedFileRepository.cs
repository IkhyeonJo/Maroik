using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEventAttachedFile table.
/// Lookup/create operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ICalendarEventAttachedFileRepository : IGenericRepository<CalendarEventAttachedFile>
{
}
