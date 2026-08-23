using Maroik.Core.Domain.Finance;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Expenditure"/> persistence.
/// </summary>
public interface IExpenditureRepository : IGenericRepository<Expenditure>
{
    /// <summary>Returns all expenditure records for the given account.</summary>
    Task<List<Expenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Returns the expenditure record identified by account email and ID, or null if not found.</summary>
    Task<Expenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);

    /// <summary>Returns expenditure records for the given account whose date falls within the given range.</summary>
    Task<List<Expenditure>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default);

    /// <summary>Returns the earliest-created expenditure record for the given account, or null if none exists.</summary>
    Task<Expenditure?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Deletes the expenditure record with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);

    /// <summary>
    /// Renames every reference to <paramref name="oldMyDepositAsset"/> in this account's expenditure
    /// records to <paramref name="newMyDepositAsset"/>. There is no foreign key on this column
    /// (transfer-less expenditures store an empty string here), so a renamed asset must be
    /// cascaded here explicitly. Returns the number of rows updated.
    /// </summary>
    Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset, CancellationToken ct = default);
}
