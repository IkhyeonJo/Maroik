using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="FixedIncome"/> domain objects,
/// representing recurring monthly income entries with a scheduled deposit day.
/// Always eager-loads the linked <see cref="OrmFixedIncome.Asset"/> so that the monetary unit
/// is available without a separate query.
/// </summary>
public class FixedIncomeRepository(ApplicationDbContext context)
    : GenericRepository<FixedIncome, OrmFixedIncome>(context), IFixedIncomeRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmFixedIncome"/> rows.</summary>
    protected override DbSet<OrmFixedIncome> Set => Context.FixedIncomes;

    // Always join the Asset navigation property so query results carry the monetary unit.
    protected override IQueryable<OrmFixedIncome> ApplyIncludes(IQueryable<OrmFixedIncome> query)
        => query.Include(x => x.Asset);

    /// <summary>Maps a persisted <see cref="OrmFixedIncome"/> row to the <see cref="FixedIncome"/> domain object.</summary>
    protected override FixedIncome ToDomain(OrmFixedIncome e) => FixedIncome.Reconstitute(
        e.Id, e.AccountEmail ?? "", e.MainClass ?? "",
        e.SubClass ?? "", e.Content, e.Amount, e.Asset?.MonetaryUnit ?? "",
        e.DepositMyAssetProductName ?? "", e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    /// <summary>Maps a <see cref="FixedIncome"/> domain object to its <see cref="OrmFixedIncome"/> persistence representation.</summary>
    protected override OrmFixedIncome ToEntity(FixedIncome fi) => new()
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

    /// <inheritdoc />
    public Task<List<FixedIncome>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    /// <inheritdoc />
    public Task<FixedIncome?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
