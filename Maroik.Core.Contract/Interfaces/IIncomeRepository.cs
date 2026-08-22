using Maroik.Core.Domain.Finance;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Income"/> persistence.
/// </summary>
public interface IIncomeRepository : IGenericRepository<Income>
{
    /// <summary>Returns all income records for the given account.</summary>
    Task<List<Income>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Returns the income record identified by account email and ID, or null if not found.</summary>
    Task<Income?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);

    /// <summary>Returns income records for the given account whose date falls within the given range.</summary>
    Task<List<Income>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Returns the earliest-created income record for the given account, or null if none exists.</summary>
    Task<Income?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Deletes the income record with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
