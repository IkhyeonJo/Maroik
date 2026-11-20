using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>Repository abstraction over <see cref="ClassModel"/> persistence: one record per account and book.</summary>
public interface IClassModelRepository : IGenericRepository<ClassModel>
{
    /// <summary>The model record of <paramref name="accountEmail"/>'s <paramref name="book"/>, or null when none was trained yet.</summary>
    Task<ClassModel?> FindAsync(string accountEmail, string book, CancellationToken ct = default);
}
