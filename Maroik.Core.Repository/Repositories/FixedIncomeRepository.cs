using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IFixedIncomeRepository"/>.</summary>
public class FixedIncomeRepository(ApplicationDbContext context) : IFixedIncomeRepository
{
    /// <inheritdoc />
    public async Task<List<FixedIncome>> GetFixedIncomesAsync(string email)
        => (await context.FixedIncomes.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = await context.FixedIncomes.AddAsync(ToEntity(fixedIncome));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = context.FixedIncomes.Update(ToEntity(fixedIncome));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = context.FixedIncomes.Remove(ToEntity(fixedIncome));
        _ = await context.SaveChangesAsync();
    }

    private static FixedIncome ToDomain(OrmFixedIncome e) => FixedIncome.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.MainClass ?? string.Empty,
        e.SubClass ?? string.Empty, e.Content, e.Amount, string.Empty,
        e.DepositMyAssetProductName ?? string.Empty, e.DepositMonth, e.DepositDay,
        e.MaturityDate, e.Note, e.Unpunctuality, e.Created, e.Updated);

    private static OrmFixedIncome ToEntity(FixedIncome fi) => new()
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
