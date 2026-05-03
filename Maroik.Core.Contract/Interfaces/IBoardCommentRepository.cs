using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for BoardComment table operations.
/// </summary>
public interface IBoardCommentRepository
{
    /// <summary>Returns all non-deleted comments belonging to the given board post, ordered by display order.</summary>
    public Task<IEnumerable<BoardComment>> GetOneBoardCommentsAsync(long boardId);

    /// <summary>Returns a single comment by its ID, or null.</summary>
    public Task<BoardComment?> GetBoardCommentAsync(int id);

    /// <summary>Batch-loads comment counts for the given set of board IDs (used for list-view badges).</summary>
    public Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds);

    /// <summary>Inserts a new comment row.</summary>
    public Task WriteBoardCommentAsync(BoardComment comment);

    /// <summary>Soft-deletes a comment (sets Deleted = true).</summary>
    public Task DeleteBoardCommentAsync(BoardComment comment);
}
