using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class BoardCommentRepository : IBoardCommentRepository
{
    private readonly ApplicationDbContext _context;

    public BoardCommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoardCommentResponse>> GetOneBoardCommentsAsync(long boardId)
    {
        return (await _context.BoardComments.Where(x => x.BoardId == boardId).OrderBy(m => m.Order).ToListAsync()).Adapt<List<BoardCommentResponse>>();
    }

    public async Task<BoardCommentResponse?> GetBoardCommentAsync(int id)
    {
        BoardComment? comment = await _context.BoardComments.Where(x => x.Id == id).FirstOrDefaultAsync();
        return comment?.Adapt<BoardCommentResponse>();
    }

    public async Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();
        Dictionary<long, int> counts = await _context.BoardComments
            .Where(x => ids.Contains(x.BoardId) && !x.Deleted)
            .GroupBy(x => x.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count);
        foreach (long id in ids.Where(id => !counts.ContainsKey(id)))
            counts[id] = 0;
        return counts;
    }

    public async Task WriteBoardCommentAsync(BoardCommentRequest request)
    {
        _ = await _context.BoardComments.AddAsync(request.Adapt<BoardComment>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task DeleteBoardCommentAsync(BoardCommentRequest request)
    {
        _ = _context.BoardComments.Update(request.Adapt<BoardComment>());
        _ = await _context.SaveChangesAsync();
    }
}
