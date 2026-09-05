using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ApplicationDbContext _context;

    public BoardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns a queryable of active (non-deleted) posts of one type.
    /// </summary>
    /// <returns></returns>
    public IQueryable<Board> QueryActiveByType(string type)
    {
        return _context.Boards.Where(x => x.Type == type && !x.Deleted);
    }

    /// <summary>
    /// Gets one active (non-deleted) post by its Id directly.
    /// </summary>
    /// <returns></returns>
    public async Task<Board> GetActiveByIdAsync(long id, string type)
    {
        return await _context.Boards.FirstOrDefaultAsync(x => x.Id == id && x.Type == type && !x.Deleted);
    }

    /// <summary>
    /// Gets one active (non-deleted) post by its Id, scoped to both type and writer.
    /// </summary>
    /// <returns></returns>
    public async Task<Board> GetActiveByIdForWriterAsync(long id, string type, string writer)
    {
        return await _context.Boards.FirstOrDefaultAsync(x => x.Id == id && x.Type == type && !x.Deleted && x.Writer == writer);
    }

    /// <summary>
    /// Gets all active (non-deleted), pinned ("Noticed") posts of one type.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Board>> GetNoticedBoardsAsync(string type)
    {
        return await _context.Boards
            .Where(x => x.Type == type && !x.Deleted && x.Noticed)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Creates a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public async Task WriteBoardAsync(Board board)
    {
        _ = await _context.Boards.AddAsync(board);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public async Task UpdateBoardAsync(Board board)
    {
        _ = _context.Boards.Update(board);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a post.
    /// </summary>
    /// <param name="board"></param>
    /// <returns></returns>
    public async Task DeleteBoardAsync(Board board)
    {
        _ = _context.Boards.Update(board);
        _ = await _context.SaveChangesAsync();
    }

}