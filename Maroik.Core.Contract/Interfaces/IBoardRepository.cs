using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for Board post table operations.
/// </summary>
public interface IBoardRepository
{
    /// <summary>Returns all non-deleted posts of the given board type (e.g. "FreeForum", "PrivateNote").</summary>
    public Task<IEnumerable<Board>> GetOneTypeBoardsAsync(string type);

    /// <summary>Returns a single post by its ID, or null if not found / deleted.</summary>
    public Task<Board?> GetBoardByIdAsync(long id);

    /// <summary>Atomically increments the view counter of the post with the given ID.</summary>
    public Task IncrementViewAsync(long id);

    /// <summary>Inserts a new board post row and returns the database-generated ID.</summary>
    public Task<long> WriteBoardAsync(Board board);

    /// <summary>Updates an existing board post (title, content, locked, noticed flags).</summary>
    public Task UpdateBoardAsync(Board board);

    /// <summary>Soft-deletes a board post (sets Deleted = true).</summary>
    public Task DeleteBoardAsync(Board board);
}
