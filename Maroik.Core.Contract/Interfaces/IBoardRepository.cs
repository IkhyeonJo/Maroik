using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for Board post table operations.
/// </summary>
public interface IBoardRepository
{
    /// <summary>Returns all non-deleted posts of the given board type (e.g. "FreeForum", "PrivateNote").</summary>
    public Task<IEnumerable<BoardResponse>> GetOneTypeBoardsAsync(string type);

    /// <summary>Returns a single post by its ID, or null if not found / deleted.</summary>
    public Task<BoardResponse?> GetBoardByIdAsync(long id);

    /// <summary>Atomically increments the view counter of the post with the given ID.</summary>
    public Task IncrementViewAsync(long id);

    /// <summary>Inserts a new board post row.</summary>
    public Task WriteBoardAsync(BoardRequest request);

    /// <summary>Updates an existing board post (title, content, locked, noticed flags).</summary>
    public Task UpdateBoardAsync(BoardRequest request);

    /// <summary>Soft-deletes a board post (sets Deleted = true).</summary>
    public Task DeleteBoardAsync(BoardRequest request);
}
