using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IBoardRepository"/>.</summary>
public class BoardRepository : IBoardRepository
{
    private readonly ApplicationDbContext _context;

    public BoardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BoardResponse>> GetOneTypeBoardsAsync(string type)
    {
        return (await _context.Boards.Where(x => x.Type == type).ToListAsync()).Adapt<List<BoardResponse>>();
    }

    /// <inheritdoc />
    public async Task<BoardResponse?> GetBoardByIdAsync(long id)
    {
        var board = await _context.Boards.FirstOrDefaultAsync(x => x.Id == id && !x.Deleted);
        return board?.Adapt<BoardResponse>();
    }

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id)
    {
        await _context.Boards
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.View, b => b.View + 1));
    }

    /// <inheritdoc />
    public async Task WriteBoardAsync(BoardRequest request)
    {
        _ = await _context.Boards.AddAsync(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateBoardAsync(BoardRequest request)
    {
        _ = _context.Boards.Update(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteBoardAsync(BoardRequest request)
    {
        _ = _context.Boards.Update(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }
}
