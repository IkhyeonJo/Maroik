using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the OtherCalendar table.
/// OtherCalendar rows represent subscriptions: an account subscribing to a calendar owned by another user.
/// </summary>
public interface IOtherCalendarRepository
{
    /// <summary>Returns all subscription rows across all accounts (admin use).</summary>
    public Task<List<OtherCalendarResponse>> GetAllOtherCalendarsAsync();

    /// <summary>Returns all calendars that the given account has subscribed to.</summary>
    public Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email);

    /// <summary>Inserts a subscription row (account subscribes to a shared calendar).</summary>
    public Task CreateOtherCalendarAsync(OtherCalendarRequest request);

    /// <summary>Removes a subscription row (account unsubscribes from a calendar).</summary>
    public Task DeleteOtherCalendarAsync(OtherCalendarRequest request);
}
