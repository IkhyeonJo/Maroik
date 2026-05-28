using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Repositories;

public class FixedIncomeRepository(ApplicationDbContext context)
    : GenericRepository<FixedIncome, OrmFixedIncome>(context), IFixedIncomeRepository
{
    protected override DbSet<OrmFixedIncome> Set => Context.FixedIncomes;

    protected override FixedIncome ToDomain(OrmFixedIncome e) => FixedIncome.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

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
}
