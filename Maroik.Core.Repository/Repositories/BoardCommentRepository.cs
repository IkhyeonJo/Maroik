using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoardComment = Maroik.Core.PostgreSQL.Models.BoardComment;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IBoardCommentRepository"/>.</summary>
public class BoardCommentRepository(ApplicationDbContext context) : IBoardCommentRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<BoardComment>> GetOneBoardCommentsAsync(long boardId)
        => (await context.BoardComments.Where(x => x.BoardId == boardId).OrderBy(m => m.Order).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<BoardComment?> GetBoardCommentAsync(int id)
    {
        OrmBoardComment? entity = await context.BoardComments.Where(x => x.Id == id).FirstOrDefaultAsync();
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();
        Dictionary<long, int> counts = await context.BoardComments
            .Where(x => ids.Contains(x.BoardId) && !x.Deleted)
            .GroupBy(x => x.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count);
        foreach (long id in ids.Where(id => !counts.ContainsKey(id)))
            counts[id] = 0;
        return counts;
    }

    /// <inheritdoc />
    public async Task WriteBoardCommentAsync(BoardComment comment)
    {
        _ = await context.BoardComments.AddAsync(ToEntity(comment));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteBoardCommentAsync(BoardComment comment)
    {
        _ = context.BoardComments.Update(ToEntity(comment));
        _ = await context.SaveChangesAsync();
    }

    private static BoardComment ToDomain(OrmBoardComment e) => BoardComment.Reconstitute(
        e.Id, e.BoardId, e.Order, e.AvatarImagePath, e.Writer ?? string.Empty,
        e.Content, e.Created, e.Deleted);

    private static OrmBoardComment ToEntity(BoardComment c) => new()
    {
        Id = c.Id,
        BoardId = c.BoardId,
        Order = c.Order,
        AvatarImagePath = c.AvatarImagePath,
        Writer = c.Writer,
        Content = c.Content,
        Created = c.Created,
        Deleted = c.Deleted
    };
}
