using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IIncomeRepository"/> that persists and retrieves
/// <see cref="Income"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class IncomeRepository(ApplicationDbContext context) : IIncomeRepository
{
    /// <inheritdoc />
    public async Task<List<Income>> GetIncomesAsync(string email)
        => (await context.Incomes.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Income>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId)
    {
        // Use a raw SQL query so that PostgreSQL's AT TIME ZONE operator can convert UTC timestamps
        // to the user's local time zone before extracting the year portion for filtering.
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
        // Same time-zone conversion as above, but filters to a specific year-month (e.g. "2024-03").
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
        // Hard-delete: the income row is physically removed from the database.
        _ = context.Incomes.Remove(ToEntity(income));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmIncome"/> row to the <see cref="Income"/> domain object,
    /// applying null-coalescing defaults for required string fields.
    /// An empty string is passed for the monetary-unit currency symbol because it is not stored
    /// on the Income table (it is derived from the associated Asset).
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Income"/> domain object.</returns>
    private static Income ToDomain(OrmIncome e) => Income.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.Note, e.Created, e.Updated);

    /// <summary>
    /// Maps an <see cref="Income"/> domain object to an ORM <see cref="OrmIncome"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="i">The domain object to convert.</param>
    /// <returns>An <see cref="OrmIncome"/> entity suitable for EF Core tracking.</returns>
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
