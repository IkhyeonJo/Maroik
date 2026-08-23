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

    /// <summary>
    /// Renames every reference to <paramref name="oldMyDepositAsset"/> in this account's fixed
    /// expenditure records to <paramref name="newMyDepositAsset"/>. There is no foreign key on
    /// this column (transfer-less fixed expenditures store an empty string here), so a renamed
    /// asset must be cascaded here explicitly. Returns the number of rows updated.
    /// </summary>
    Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset, CancellationToken ct = default);
}
