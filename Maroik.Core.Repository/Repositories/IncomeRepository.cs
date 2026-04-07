using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IIncomeRepository"/>.</summary>
public class IncomeRepository(ApplicationDbContext context) : IIncomeRepository
{
    /// <inheritdoc />
    public async Task<List<IncomeResponse>> GetIncomesAsync(string email)
    {
        return (await context.Incomes.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<IncomeResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<IncomeResponse>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId)
    {
        return (await context.Incomes.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Income"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Adapt<List<IncomeResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<IncomeResponse>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        return (await context.Incomes.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Income"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Adapt<List<IncomeResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateIncomeAsync(IncomeRequest request)
    {
        _ = await context.Incomes.AddAsync(request.Adapt<Income>());
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateIncomeAsync(IncomeRequest request)
    {
        _ = context.Incomes.Update(request.Adapt<Income>());
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteIncomeAsync(IncomeRequest request)
    {
        _ = context.Incomes.Remove(request.Adapt<Income>());
        _ = await context.SaveChangesAsync();
    }
}
