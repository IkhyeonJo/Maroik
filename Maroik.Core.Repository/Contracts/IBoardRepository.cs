using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IBoardRepository
{
    /// <summary>
    /// Gets all posts of one type.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<Board>> GetOneTypeBoardsAsync(string type);

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