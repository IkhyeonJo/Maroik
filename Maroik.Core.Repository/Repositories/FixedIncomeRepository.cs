using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IFixedIncomeRepository"/> that persists and retrieves
/// <see cref="FixedIncome"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class FixedIncomeRepository(ApplicationDbContext context) : IFixedIncomeRepository
{
    /// <inheritdoc />
    public async Task<List<FixedIncome>> GetFixedIncomesAsync(string email)
        => (await context.FixedIncomes.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = await context.FixedIncomes.AddAsync(ToEntity(fixedIncome));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        var existing = await context.FixedIncomes.FindAsync(fixedIncome.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(fixedIncome));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedIncomeAsync(FixedIncome fixedIncome)
    {
        // Hard-delete: the fixed income row is physically removed from the database.
        var existing = await context.FixedIncomes.FindAsync(fixedIncome.Id);
        if (existing == null) return;
        _ = context.FixedIncomes.Remove(existing);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmFixedIncome"/> row to the <see cref="FixedIncome"/> domain object,
    /// applying null-coalescing defaults for required string fields.
    /// An empty string is passed for the monetary-unit currency symbol because it is not stored
    /// on the FixedIncome table (it is derived from the associated Asset).
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="FixedIncome"/> domain object.</returns>
    private static FixedIncome ToDomain(OrmFixedIncome e) => FixedIncome.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    /// <summary>
    /// Maps a <see cref="FixedIncome"/> domain object to an ORM <see cref="OrmFixedIncome"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="fi">The domain object to convert.</param>
    /// <returns>An <see cref="OrmFixedIncome"/> entity suitable for EF Core tracking.</returns>
    private static OrmFixedIncome ToEntity(FixedIncome fi) => new()
    {
        Id = fi.Id,
        AccountEmail = fi.AccountEmail.Value,
        MainClass = fi.MainClass,
        SubClass = fi.SubClass,
        Content = fi.Content,
        Amount = fi.Amount.Amount,
        DepositMyAssetProductName = fi.DepositMyAssetProductName,
        DepositMonth = fi.DepositMonth,
        DepositDay = fi.DepositDay,
        MaturityDate = fi.MaturityDate,
        Note = fi.Note,
        Unpunctuality = fi.Unpunctuality,
        Created = fi.Created,
        Updated = fi.Updated
    };
}
