using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IIncomeRepository"/>.</summary>
public class IncomeRepository(ApplicationDbContext context) : IIncomeRepository
{
    /// <inheritdoc />
    public async Task<List<Income>> GetIncomesAsync(string email)
        => (await context.Incomes.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Income>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId)
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
        ).ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Income>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId)
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
        ).ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task CreateIncomeAsync(Income income)
    {
        _ = await context.Incomes.AddAsync(ToEntity(income));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateIncomeAsync(Income income)
    {
        _ = context.Incomes.Update(ToEntity(income));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteIncomeAsync(Income income)
    {
        _ = context.Incomes.Remove(ToEntity(income));
        _ = await context.SaveChangesAsync();
    }

    private static Income ToDomain(OrmIncome e) => Income.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.Note, e.Created, e.Updated);

    private static OrmIncome ToEntity(Income i) => new()
    {
        Id = i.Id,
        AccountEmail = i.AccountEmail.Value,
        MainClass = i.MainClass,
        SubClass = i.SubClass,
        Content = i.Content,
        Amount = i.Amount.Amount,
        DepositMyAssetProductName = i.DepositMyAssetProductName,
        Note = i.Note,
        Created = i.Created,
        Updated = i.Updated
    };
}
