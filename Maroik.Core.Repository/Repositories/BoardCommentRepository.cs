using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class BoardCommentRepository : IBoardCommentRepository
{
    private readonly ApplicationDbContext _context;

    public BoardCommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all comments for a post.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<BoardComment>> GetOneBoardCommentsAsync(long boardId)
    {
        return await _context.BoardComments.Where(x => x.BoardId == boardId).OrderBy(m => m.Order).ToListAsync();
    }

    /// <summary>
    /// Gets a comment.
    /// </summary>
    /// <returns></returns>
    public async Task<BoardComment> GetBoardCommentAsync(int id)
    {
        return await _context.BoardComments.Where(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Creates a comment.
    /// </summary>
    /// <param name="boardComment"></param>
    /// <returns></returns>
    public async Task WriteBoardCommentAsync(BoardComment boardComment)
    {
        _ = await _context.BoardComments.AddAsync(boardComment);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a comment.
    /// </summary>
    /// <param name="boardComment"></param>
    /// <returns></returns>
    public async Task DeleteBoardCommentAsync(BoardComment boardComment)
    {
        _ = _context.BoardComments.Update(boardComment);
        _ = await _context.SaveChangesAsync();
    }

}