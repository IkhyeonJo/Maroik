using Maroik.Core.Contract.Domains.Finance;
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
    protected override DbSet<OrmExpenditure> Set => Context.Expenditures;

    // Always join the Asset navigation property so query results carry the monetary unit.
    protected override IQueryable<OrmExpenditure> ApplyIncludes(IQueryable<OrmExpenditure> query)
        => query.Include(x => x.Asset);

    protected override Expenditure ToDomain(OrmExpenditure e) => Expenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, e.Asset?.MonetaryUnit ?? string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.Note,
        e.Created, e.Updated);

    protected override OrmExpenditure ToEntity(Expenditure e) => new()
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

    public Task<List<Expenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    public Task<Expenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    public Task<List<Expenditure>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail && e.Created >= from && e.Created < to, ct: ct);

    public async Task<Expenditure?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default)
    {
        var entity = await Set.Where(e => e.AccountEmail == accountEmail).OrderBy(e => e.Created).FirstOrDefaultAsync(ct);
        return entity == null ? null : ToDomain(entity);
    }

    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);

    public async Task UpdateMyDepositAssetNameAsync(string accountEmail, string oldName, string newName, CancellationToken ct = default)
    {
        // Context uses NoTracking + AutoDetectChangesEnabled=false, so LINQ-tracked update won't work.
        // ExecuteUpdateAsync issues a direct SQL UPDATE, bypassing the change tracker entirely.
        _ = await Context.Expenditures
            .Where(e => e.AccountEmail == accountEmail && e.MyDepositAsset == oldName)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.MyDepositAsset, newName), ct);
    }
}
