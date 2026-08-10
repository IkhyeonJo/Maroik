using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Income"/> domain objects.
/// Always eager-loads the linked <see cref="OrmIncome.Asset"/> so that the monetary unit
/// is available without a separate query.
/// </summary>
public class IncomeRepository(ApplicationDbContext context)
    : GenericRepository<Income, OrmIncome>(context), IIncomeRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmIncome"/> rows.</summary>
    protected override DbSet<OrmIncome> Set => Context.Incomes;

    // Always join the Asset navigation property so query results carry the monetary unit.
    protected override IQueryable<OrmIncome> ApplyIncludes(IQueryable<OrmIncome> query)
        => query.Include(x => x.Asset);

    /// <summary>Maps a persisted <see cref="OrmIncome"/> row to the <see cref="Income"/> domain object.</summary>
    protected override Income ToDomain(OrmIncome e) => Income.Reconstitute(
        e.Id, e.AccountEmail ?? "", e.MainClass ?? "",
        e.SubClass ?? "", e.Content, e.Amount, e.Asset?.MonetaryUnit ?? "",
        e.DepositMyAssetProductName ?? "", e.Note, e.Created, e.Updated);

    /// <summary>Maps an <see cref="Income"/> domain object to its <see cref="OrmIncome"/> persistence representation.</summary>
    protected override OrmIncome ToEntity(Income i) => new()
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

    /// <inheritdoc />
    public Task<List<Income>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    /// <inheritdoc />
    public Task<Income?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    /// <inheritdoc />
    public Task<List<Income>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail && e.Created >= from && e.Created < to, ct: ct);

    /// <inheritdoc />
    public async Task<Income?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default)
    {
        var entity = await Set.Where(e => e.AccountEmail == accountEmail).OrderBy(e => e.Created).FirstOrDefaultAsync(ct);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
