using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Income"/> domain objects.
/// </summary>
public class IncomeRepository(ApplicationDbContext context)
    : GenericRepository<Income, OrmIncome>(context), IIncomeRepository
{
    protected override DbSet<OrmIncome> Set => Context.Incomes;

    protected override Income ToDomain(OrmIncome e) => Income.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.Note, e.Created, e.Updated);

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

    public Task<List<Income>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    public Task<Income?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    public Task<List<Income>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail && e.Created >= from && e.Created < to, ct: ct);

    public async Task<Income?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default)
    {
        var entity = await Set.Where(e => e.AccountEmail == accountEmail).OrderBy(e => e.Created).FirstOrDefaultAsync(ct);
        return entity == null ? null : ToDomain(entity);
    }

    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
