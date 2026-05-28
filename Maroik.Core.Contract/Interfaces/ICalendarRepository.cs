using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Calendar table.
/// All CRUD operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ICalendarRepository : IGenericRepository<Calendar>
{
}
