using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IFixedIncomeRepository"/>.</summary>
public class FixedIncomeRepository : IFixedIncomeRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="FixedIncomeRepository"/> with <paramref name="context"/>.</summary>
    public FixedIncomeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string email)
    {
        return (await _context.FixedIncomes.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<FixedIncomeResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateFixedIncomeAsync(FixedIncomeRequest request)
    {
        _ = await _context.FixedIncomes.AddAsync(request.Adapt<FixedIncome>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedIncomeAsync(FixedIncomeRequest request)
    {
        _ = _context.FixedIncomes.Update(request.Adapt<FixedIncome>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedIncomeAsync(FixedIncomeRequest request)
    {
        _ = _context.FixedIncomes.Remove(request.Adapt<FixedIncome>());
        _ = await _context.SaveChangesAsync();
    }
}
