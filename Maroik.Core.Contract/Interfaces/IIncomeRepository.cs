using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IIncomeRepository
{
    public Task<List<IncomeResponse>> GetIncomesAsync(string email);
    public Task<List<IncomeResponse>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId);
    public Task<List<IncomeResponse>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId);
    public Task CreateIncomeAsync(IncomeRequest request);
    public Task UpdateIncomeAsync(IncomeRequest request);
    public Task DeleteIncomeAsync(IncomeRequest request);
}
