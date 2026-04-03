using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class BoardAttachedFileRepository : IBoardAttachedFileRepository
{
    private readonly ApplicationDbContext _context;

    public BoardAttachedFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Gets all registered accounts.
    /// </summary>
    /// <returns></returns>
    public async Task<List<BoardAttachedFile>> GetAllAsync()
    {
        return await _context.BoardAttachedFiles.ToListAsync();
    }

    /// <summary>
    /// Saves an attached file.
    /// </summary>
    /// <param name="boardAttachedFile"></param>
    /// <returns></returns>
    public async Task SaveBoardAttachedFileAsync(BoardAttachedFile boardAttachedFile)
    {
        _ = await _context.BoardAttachedFiles.AddAsync(boardAttachedFile);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an attached file.
    /// </summary>
    /// <param name="boardAttachedFile"></param>
    /// <returns></returns>
    public async Task UpdateBoardAttachedFileAsync(BoardAttachedFile boardAttachedFile)
    {
        _ = _context.BoardAttachedFiles.Update(boardAttachedFile);
        _ = await _context.SaveChangesAsync();
    }
}