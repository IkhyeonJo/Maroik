using Maroik.Core.Contract.Domains.Board;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Board"/> (forum / private-note post) persistence.
/// </summary>
public interface IBoardRepository : IGenericRepository<Board>
{
    /// <summary>Increments the view counter of the board post by 1.</summary>
    Task IncrementViewAsync(long id, CancellationToken ct = default);

    /// <summary>Inserts a new board post and returns its generated ID.</summary>
    Task<long> WriteBoardAsync(Board board, CancellationToken ct = default);

    /// <summary>Returns all posts of the specified board type.</summary>
    Task<List<Board>> GetByTypeAsync(string type, CancellationToken ct = default);

    /// <summary>Returns the non-deleted post with the given ID, or null if not found.</summary>
    Task<Board?> FindActiveByIdAsync(long id, CancellationToken ct = default);
}
