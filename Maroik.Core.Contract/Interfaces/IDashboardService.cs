using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string accountEmail, string year, string month, string timeZoneId);
    Task UpdateDefaultMonetaryUnitAsync(string accountEmail, string? monetaryUnit);
}
