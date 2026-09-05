using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IBoardRepository
{
    /// <summary>
    /// Returns a queryable of active (non-deleted) posts of one type, for building
    /// list/search/pagination queries that get pushed down to SQL instead of
    /// materialized into memory.
    /// </summary>
    /// <returns></returns>
    public IQueryable<Board> QueryActiveByType(string type);

    /// <summary>
    /// Gets one active (non-deleted) post by its Id, scoped to a specific type
    /// (Board.Id is a global PK across all board types — the type filter prevents
    /// e.g. a PrivateNote Id from resolving through a FreeForum lookup).
    /// </summary>
    /// <returns></returns>
    public Task<Board> GetActiveByIdAsync(long id, string type);

    /// <summary>
    /// Gets one active (non-deleted) post by its Id, scoped to both type and a
    /// specific writer (e.g. PrivateNote ownership) — both filters are applied in
    /// the query itself, not as a post-fetch check, so together they are the
    /// access-control boundary.
    /// </summary>
    /// <returns></returns>
    public Task<Board> GetActiveByIdForWriterAsync(long id, string type, string writer);

    /// <summary>
    /// Gets all active (non-deleted), pinned ("Noticed") posts of one type.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<Board>> GetNoticedBoardsAsync(string type);

    /// <summary>
    /// Creates a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public Task WriteBoardAsync(Board board);

    /// <summary>
    /// Updates a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public Task UpdateBoardAsync(Board board);

    /// <summary>
    /// Deletes a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public Task DeleteBoardAsync(Board board);

}