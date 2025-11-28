using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ApplicationDbContext _context;

    public BoardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all posts of one type.
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<Board>> GetOneTypeBoardsAsync(string type)
    {
        return await _context.Boards.Where(x => x.Type == type).ToListAsync();
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