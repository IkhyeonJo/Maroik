using Maroik.Core.Repository.Contracts;
using Maroik.Core.Repository.Data;
using Maroik.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class FixedIncomeRepository : IFixedIncomeRepository
{
    private readonly ApplicationDbContext _context;

    public FixedIncomeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all fixed incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<FixedIncome>> GetFixedIncomesAsync(string email)
    {
        return await _context.FixedIncomes.Where(x => x.AccountEmail == email).ToListAsync();
    }

    /// <summary>
    /// Creates a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public async Task CreateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = await _context.FixedIncomes.AddAsync(fixedIncome);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public async Task UpdateFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = _context.FixedIncomes.Update(fixedIncome);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public async Task DeleteFixedIncomeAsync(FixedIncome fixedIncome)
    {
        _ = _context.FixedIncomes.Remove(fixedIncome);
        _ = await _context.SaveChangesAsync();
    }
}