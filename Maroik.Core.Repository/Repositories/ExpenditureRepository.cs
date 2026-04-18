using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IExpenditureRepository"/>.</summary>
public class ExpenditureRepository : IExpenditureRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="ExpenditureRepository"/> with <paramref name="context"/>.</summary>
    public ExpenditureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<ExpenditureResponse>> GetExpendituresAsync(string email)
    {
        return (await _context.Expenditures.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<ExpenditureResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<ExpenditureResponse>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId)
    {
        return (await _context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Adapt<List<ExpenditureResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<ExpenditureResponse>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        return (await _context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Adapt<List<ExpenditureResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateExpenditureAsync(ExpenditureRequest request)
    {
        _ = await _context.Expenditures.AddAsync(request.Adapt<Expenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateExpenditureAsync(ExpenditureRequest request)
    {
        _ = _context.Expenditures.Update(request.Adapt<Expenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteExpenditureAsync(ExpenditureRequest request)
    {
        _ = _context.Expenditures.Remove(request.Adapt<Expenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
              UPDATE "Expenditure"
              SET "MyDepositAsset" = {newMyDepositAsset}
              WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
              """
        );
    }
}
