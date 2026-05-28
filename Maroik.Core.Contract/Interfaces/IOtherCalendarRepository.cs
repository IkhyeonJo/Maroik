using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the OtherCalendar (calendar subscription) table.
/// All CRUD operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IOtherCalendarRepository : IGenericRepository<OtherCalendar> { }
