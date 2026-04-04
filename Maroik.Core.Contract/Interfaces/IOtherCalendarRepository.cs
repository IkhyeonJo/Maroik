using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IOtherCalendarRepository
{
    public Task<List<OtherCalendarResponse>> GetAllOtherCalendarsAsync();
    public Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email);
    public Task CreateOtherCalendarAsync(OtherCalendarRequest request);
    public Task DeleteOtherCalendarAsync(OtherCalendarRequest request);
}
