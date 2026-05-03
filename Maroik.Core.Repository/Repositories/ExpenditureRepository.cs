using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IExpenditureRepository"/>.</summary>
public class ExpenditureRepository(ApplicationDbContext context) : IExpenditureRepository
{
    /// <inheritdoc />
    public async Task<List<Expenditure>> GetExpendituresAsync(string email)
        => (await context.Expenditures.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Expenditure>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId)
    {
        return (await context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        return (await context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task CreateExpenditureAsync(Expenditure expenditure)
    {
        _ = await context.Expenditures.AddAsync(ToEntity(expenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateExpenditureAsync(Expenditure expenditure)
    {
        _ = context.Expenditures.Update(ToEntity(expenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteExpenditureAsync(Expenditure expenditure)
    {
        _ = context.Expenditures.Remove(ToEntity(expenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        return await context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
              UPDATE "Expenditure"
              SET "MyDepositAsset" = {newMyDepositAsset}
              WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
              """
        );
    }

    private static Expenditure ToDomain(OrmExpenditure e) => Expenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.Note,
        e.Created, e.Updated);

    private static OrmExpenditure ToEntity(Expenditure e) => new()
    {
        Id = e.Id,
        AccountEmail = e.AccountEmail.Value,
        MainClass = e.MainClass,
        SubClass = e.SubClass,
        Content = e.Content,
        Amount = e.Amount.Amount,
        PaymentMethod = e.PaymentMethod,
        MyDepositAsset = e.MyDepositAsset,
        Note = e.Note,
        Created = e.Created,
        Updated = e.Updated
    };
}
