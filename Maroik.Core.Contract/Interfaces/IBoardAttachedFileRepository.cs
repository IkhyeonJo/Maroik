using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the BoardAttachedFile table.
/// Lookup/create operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IBoardAttachedFileRepository : IGenericRepository<BoardAttachedFile>
{
    /// <summary>Batch-loads attached files for multiple board IDs; value is null when no file is attached.</summary>
    Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds);
}
