using Maroik.Core.Domain.Finance;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="FixedExpenditure"/> (recurring expenditure) persistence.
/// </summary>
public interface IFixedExpenditureRepository : IGenericRepository<FixedExpenditure>
{
    /// <summary>Returns all fixed expenditure records for the given account.</summary>
    Task<List<FixedExpenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Returns the fixed expenditure record identified by account email and ID, or null if not found.</summary>
    Task<FixedExpenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);

    /// <summary>Deletes the fixed expenditure record with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Renames the deposit-asset reference on all accounts fixed expenditure records (cascades an asset product-name change).</summary>
    Task UpdateMyDepositAssetNameAsync(string accountEmail, string oldName, string newName, CancellationToken ct = default);
}
