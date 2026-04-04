using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class BoardRepository : IBoardRepository
{
    private readonly ApplicationDbContext _context;

    public BoardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardResponse>> GetOneTypeBoardsAsync(string type)
    {
        return (await _context.Boards.Where(x => x.Type == type).ToListAsync()).Adapt<List<BoardResponse>>();
    }

    public async Task WriteBoardAsync(BoardRequest request)
    {
        _ = await _context.Boards.AddAsync(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateBoardAsync(BoardRequest request)
    {
        _ = _context.Boards.Update(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task DeleteBoardAsync(BoardRequest request)
    {
        _ = _context.Boards.Update(request.Adapt<Board>());
        _ = await _context.SaveChangesAsync();
    }
}
