using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Expenditure"/> domain objects.
/// Always eager-loads the linked <see cref="OrmExpenditure.Asset"/> so that the monetary unit
/// is available without a separate query.
/// </summary>
public class ExpenditureRepository(ApplicationDbContext context)
    : GenericRepository<Expenditure, OrmExpenditure>(context), IExpenditureRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmExpenditure"/> rows.</summary>
    protected override DbSet<OrmExpenditure> Set => Context.Expenditures;

    /// <inheritdoc />
    // Always join the Asset navigation property so query results carry the monetary unit.
    protected override IQueryable<OrmExpenditure> ApplyIncludes(IQueryable<OrmExpenditure> query)
        => query.Include(x => x.Asset);

    /// <summary>Maps a persisted <see cref="OrmExpenditure"/> row to the <see cref="Expenditure"/> domain object.</summary>
    protected override Expenditure ToDomain(OrmExpenditure e) => Expenditure.Reconstitute(
        e.Id, e.AccountEmail ?? "", e.MainClass ?? "",
        e.SubClass ?? "", e.Content, e.Amount, e.Asset?.MonetaryUnit ?? "",
        e.PaymentMethod ?? "", e.MyDepositAsset, e.Note,
        e.Created, e.Updated);

    /// <summary>Maps an <see cref="Expenditure"/> domain object to its <see cref="OrmExpenditure"/> persistence representation.</summary>
    protected override OrmExpenditure ToEntity(Expenditure e) => new()
    {
        Id = e.Id,
        AccountEmail = e.AccountEmail.Value,
        MainClass = e.MainClass,
        SubClass = e.SubClass,
        Content = e.Content ?? "",
        Amount = e.Amount.Amount,
        PaymentMethod = e.PaymentMethod,
        MyDepositAsset = e.MyDepositAsset,
        Note = e.Note ?? "",
        Created = e.Created,
        Updated = e.Updated
    };

    /// <inheritdoc />
    public Task<List<Expenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    /// <inheritdoc />
    public Task<Expenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    /// <inheritdoc />
    public Task<List<Expenditure>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail && e.Created >= from && e.Created < to, ct: ct);

    /// <inheritdoc />
    public async Task<Expenditure?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default)
    {
        var entity = await Set.Where(e => e.AccountEmail == accountEmail).OrderBy(e => e.Created).FirstOrDefaultAsync(ct);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset, CancellationToken ct = default)
        => Set
            .Where(e => e.AccountEmail == accountEmail && e.MyDepositAsset == oldMyDepositAsset)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.MyDepositAsset, newMyDepositAsset), ct);
}
