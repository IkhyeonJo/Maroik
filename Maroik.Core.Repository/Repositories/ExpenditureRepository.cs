using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IExpenditureRepository"/> that persists and retrieves
/// <see cref="Expenditure"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class ExpenditureRepository(ApplicationDbContext context) : IExpenditureRepository
{
    /// <inheritdoc />
    public async Task<List<Expenditure>> GetExpendituresAsync(string email)
        => (await context.Expenditures
            .Include(x => x.Asset)
            .Where(x => x.AccountEmail == email)
            .ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Expenditure>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId)
    {
        // Use a raw SQL query so that PostgreSQL's AT TIME ZONE operator can convert UTC timestamps
        // to the user's local time zone before extracting the year portion for filtering.
        return (await context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).Include(x => x.Asset).ToListAsync()).Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        // Same time-zone conversion as above, but filters to a specific year-month (e.g. "2024-03").
        return (await context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).Include(x => x.Asset).ToListAsync()).Select(ToDomain).ToList();
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
        // Hard-delete: the expenditure row is physically removed from the database.
        _ = context.Expenditures.Remove(ToEntity(expenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        // Raw SQL is used to rename the deposit-asset reference across all matching expenditure rows
        // without loading each row into memory individually.
        return await context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
              UPDATE "Expenditure"
              SET "MyDepositAsset" = {newMyDepositAsset}
              WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
              """
        );
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmExpenditure"/> row to the <see cref="Expenditure"/> domain object.
    /// Currency is read from the joined <see cref="OrmExpenditure.Asset"/> navigation property;
    /// falls back to an empty string when the join yields no row (e.g. asset was hard-deleted).
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Expenditure"/> domain object.</returns>
    private static Expenditure ToDomain(OrmExpenditure e) => Expenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, e.Asset?.MonetaryUnit ?? string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.Note,
        e.Created, e.Updated);

    /// <summary>
    /// Maps an <see cref="Expenditure"/> domain object to an ORM <see cref="OrmExpenditure"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="e">The domain object to convert.</param>
    /// <returns>An <see cref="OrmExpenditure"/> entity suitable for EF Core tracking.</returns>
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
