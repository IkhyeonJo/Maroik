using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for BoardComment table operations.
/// Lookup/create operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IBoardCommentRepository : IGenericRepository<BoardComment>
{
    /// <summary>Batch-loads comment counts for the given set of board IDs (used for list-view badges).</summary>
    Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct = default);
}
