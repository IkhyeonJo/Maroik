using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CalendarSharedMapper
{
    internal static CalendarSharedResponse ToResponse(CalendarShared shared) => new()
    {
        CalendarId = shared.Id,
        User = shared.User,
        Anonymous = shared.Anonymous
    };
}
