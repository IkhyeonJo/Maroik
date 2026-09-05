using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="FixedExpenditure"/> domain objects,
/// representing recurring monthly expenditures with a scheduled deposit day.
/// Always eager-loads the linked <see cref="OrmFixedExpenditure.Asset"/> so that the monetary unit
/// is available without a separate query.
/// </summary>
public class FixedExpenditureRepository(ApplicationDbContext context)
    : GenericRepository<FixedExpenditure, OrmFixedExpenditure>(context), IFixedExpenditureRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmFixedExpenditure"/> rows.</summary>
    protected override DbSet<OrmFixedExpenditure> Set => Context.FixedExpenditures;

    /// <inheritdoc />
    /// <inheritdoc />
    // Always join the Asset navigation property so query results carry the monetary unit.
    protected override IQueryable<OrmFixedExpenditure> ApplyIncludes(IQueryable<OrmFixedExpenditure> query)
        => query.Include(x => x.Asset);

    /// <summary>Maps a persisted <see cref="OrmFixedExpenditure"/> row to the <see cref="FixedExpenditure"/> domain object.</summary>
    protected override FixedExpenditure ToDomain(OrmFixedExpenditure e) => FixedExpenditure.Reconstitute(
        e.Id, e.AccountEmail ?? "", e.MainClass ?? "",
        e.SubClass ?? "", e.Content, e.Amount, e.Asset?.MonetaryUnit ?? "",
        e.PaymentMethod ?? "", e.MyDepositAsset, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    /// <summary>Maps a <see cref="FixedExpenditure"/> domain object to its <see cref="OrmFixedExpenditure"/> persistence representation.</summary>
    protected override OrmFixedExpenditure ToEntity(FixedExpenditure fe) => new()
    {
        Id = fe.Id,
        AccountEmail = fe.AccountEmail.Value,
        MainClass = fe.MainClass,
        SubClass = fe.SubClass,
        Content = fe.Content ?? "",
        Amount = fe.Amount.Amount,
        PaymentMethod = fe.PaymentMethod,
        MyDepositAsset = fe.MyDepositAsset,
        DepositMonth = fe.DepositMonth,
        DepositDay = fe.DepositDay,
        MaturityDate = fe.MaturityDate,
        Note = fe.Note ?? "",
        Unpunctuality = fe.Unpunctuality,
        Created = fe.Created,
        Updated = fe.Updated
    };

    /// <inheritdoc />
    public Task<List<FixedExpenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    /// <inheritdoc />
    public Task<FixedExpenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset, CancellationToken ct = default)
        => Set
            .Where(e => e.AccountEmail == accountEmail && e.MyDepositAsset == oldMyDepositAsset)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.MyDepositAsset, newMyDepositAsset), ct);
}
