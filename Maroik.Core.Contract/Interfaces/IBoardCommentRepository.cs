using Maroik.Core.Domain.Board;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="BoardComment"/> persistence.
/// </summary>
public interface IBoardCommentRepository : IGenericRepository<BoardComment>
{
    /// <summary>Batch-counts comments for multiple board IDs. Returns a dictionary keyed by board ID.</summary>
    Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct = default);

    /// <summary>Returns all comments for the given board post in display order.</summary>
    Task<List<BoardComment>> GetByBoardIdOrderedAsync(long boardId, CancellationToken ct = default);

    /// <summary>Returns the comment with the given ID, or null if not found.</summary>
    Task<BoardComment?> FindByIdAsync(long id, CancellationToken ct = default);
}
