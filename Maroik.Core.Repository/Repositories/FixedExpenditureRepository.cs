using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IFixedExpenditureRepository"/>.</summary>
public class FixedExpenditureRepository(ApplicationDbContext context) : IFixedExpenditureRepository
{
    /// <inheritdoc />
    public async Task<List<FixedExpenditure>> GetFixedExpendituresAsync(string email)
        => (await context.FixedExpenditures.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = await context.FixedExpenditures.AddAsync(ToEntity(fixedExpenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = context.FixedExpenditures.Update(ToEntity(fixedExpenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = context.FixedExpenditures.Remove(ToEntity(fixedExpenditure));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        return await context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "FixedExpenditure"
             SET "MyDepositAsset" = {newMyDepositAsset}
             WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
             """
        );
    }

    private static FixedExpenditure ToDomain(OrmFixedExpenditure e) => FixedExpenditure.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.PaymentMethod ?? string.Empty, e.MyDepositAsset, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    private static OrmFixedExpenditure ToEntity(FixedExpenditure fe) => new()
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
}
