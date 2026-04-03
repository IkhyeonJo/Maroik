using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardCommentRepository
{
    /// <summary>
    /// Gets all comments for a post.
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<BoardComment>> GetOneBoardCommentsAsync(long boardId);

    /// <summary>
    /// Gets a comment.
    /// </summary>
    /// <returns></returns>
    public Task<BoardComment> GetBoardCommentAsync(int id);

    /// <summary>
    /// Creates a comment.
    /// </summary>
    /// <param name="boardComment"></param>
    /// <returns></returns>
    public Task WriteBoardCommentAsync(BoardComment boardComment);

    /// <summary>
    /// Deletes a comment.
    /// </summary>
    /// <param name="boardComment"></param>
    /// <returns></returns>
    public Task DeleteBoardCommentAsync(BoardComment boardComment);

}