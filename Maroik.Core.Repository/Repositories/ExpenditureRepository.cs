using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

namespace Maroik.Core.Repository.Repositories;

public class ExpenditureRepository(ApplicationDbContext context)
    : GenericRepository<Expenditure, OrmExpenditure>(context), IExpenditureRepository
{
    protected override DbSet<OrmExpenditure> Set => Context.Expenditures;

    // Always join the Asset navigation property so FindAsync results carry the monetary unit.
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
}
