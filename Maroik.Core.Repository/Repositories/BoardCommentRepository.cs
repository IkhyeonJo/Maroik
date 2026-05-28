using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoardComment = Maroik.Core.PostgreSQL.Models.BoardComment;

namespace Maroik.Core.Repository.Repositories;

public class BoardCommentRepository(ApplicationDbContext context)
    : GenericRepository<BoardComment, OrmBoardComment>(context), IBoardCommentRepository
{
    protected override DbSet<OrmBoardComment> Set => Context.BoardComments;

    /// <inheritdoc />
    public async Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();

        Dictionary<long, int> counts = await Context.BoardComments
            .Where(x => ids.Contains(x.BoardId) && !x.Deleted)
            .GroupBy(x => x.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count);

        foreach (long id in ids.Where(id => !counts.ContainsKey(id)))
            counts[id] = 0;

        return counts;
    }

    protected override BoardComment ToDomain(OrmBoardComment e) => BoardComment.Reconstitute(
        e.Id, e.BoardId, e.Order, e.AvatarImagePath, e.Writer ?? string.Empty,
        e.Content, e.Created, e.Deleted);

    protected override OrmBoardComment ToEntity(BoardComment c) => new()
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
