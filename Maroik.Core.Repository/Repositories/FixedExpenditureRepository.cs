using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFixedExpenditureRepository"/> that persists and retrieves
/// <see cref="FixedExpenditure"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class FixedExpenditureRepository(ApplicationDbContext context) : IFixedExpenditureRepository
{
    /// <inheritdoc />
    public async Task<List<FixedExpenditure>> GetFixedExpendituresAsync(string email)
        => (await context.FixedExpenditures.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = await context.FixedExpenditures.AddAsync(ToEntity(fixedExpenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        var existing = await context.FixedExpenditures.FindAsync(fixedExpenditure.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(fixedExpenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        // Hard-delete: the fixed expenditure row is physically removed from the database.
        var existing = await context.FixedExpenditures.FindAsync(fixedExpenditure.Id);
        if (existing == null) return;
        _ = context.FixedExpenditures.Remove(existing);
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        // Raw SQL renames the deposit-asset reference across all matching fixed-expenditure rows
        // without loading each row into memory individually.
        return await context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "FixedExpenditure"
             SET "MyDepositAsset" = {newMyDepositAsset}
             WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
             """
        );
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmFixedExpenditure"/> row to the <see cref="FixedExpenditure"/> domain object,
    /// applying null-coalescing defaults for required string fields.
    /// An empty string is passed for the monetary-unit currency symbol because it is not stored
    /// on the FixedExpenditure table (it is derived from the associated Asset).
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="FixedExpenditure"/> domain object.</returns>
    private static FixedExpenditure ToDomain(OrmFixedExpenditure e) => FixedExpenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    /// <summary>
    /// Maps a <see cref="FixedExpenditure"/> domain object to an ORM <see cref="OrmFixedExpenditure"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="fe">The domain object to convert.</param>
    /// <returns>An <see cref="OrmFixedExpenditure"/> entity suitable for EF Core tracking.</returns>
    private static OrmFixedExpenditure ToEntity(FixedExpenditure fe) => new()
    {
        Id = fe.Id,
        AccountEmail = fe.AccountEmail.Value,
        MainClass = fe.MainClass,
        SubClass = fe.SubClass,
        Content = fe.Content,
        Amount = fe.Amount.Amount,
        PaymentMethod = fe.PaymentMethod,
        MyDepositAsset = fe.MyDepositAsset,
        DepositMonth = fe.DepositMonth,
        DepositDay = fe.DepositDay,
        MaturityDate = fe.MaturityDate,
        Note = fe.Note,
        Unpunctuality = fe.Unpunctuality,
        Created = fe.Created,
        Updated = fe.Updated
    };
}
