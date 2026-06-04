using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="FixedExpenditure"/> domain objects,
/// representing recurring monthly expenditures with a scheduled deposit day.
/// </summary>
public class FixedExpenditureRepository(ApplicationDbContext context)
    : GenericRepository<FixedExpenditure, OrmFixedExpenditure>(context), IFixedExpenditureRepository
{
    protected override DbSet<OrmFixedExpenditure> Set => Context.FixedExpenditures;

    protected override FixedExpenditure ToDomain(OrmFixedExpenditure e) => FixedExpenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    protected override OrmFixedExpenditure ToEntity(FixedExpenditure fe) => new()
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

    public Task<List<FixedExpenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    public Task<FixedExpenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.Id == id, ct);

    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);

    public async Task UpdateMyDepositAssetNameAsync(string accountEmail, string oldName, string newName, CancellationToken ct = default)
    {
        var entities = await Context.FixedExpenditures
            .Where(e => e.AccountEmail == accountEmail && e.MyDepositAsset == oldName)
            .ToListAsync(ct);
        foreach (var e in entities) e.MyDepositAsset = newName;
        _ = await Context.SaveChangesAsync(ct);
    }
}
