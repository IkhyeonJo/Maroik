using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for Board post table operations.
/// Lookup operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IBoardRepository : IGenericRepository<Board>
{
    /// <summary>Atomically increments the view counter of the post with the given ID.</summary>
    Task IncrementViewAsync(long id);

    /// <summary>Inserts a new board post row and returns the database-generated ID.</summary>
    Task<long> WriteBoardAsync(Board board);
}
