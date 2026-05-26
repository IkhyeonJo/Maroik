using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoardComment = Maroik.Core.PostgreSQL.Models.BoardComment;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBoardCommentRepository"/> that persists and retrieves
/// <see cref="BoardComment"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // Materialise to a list so the same collection can be used in both the query and the gap-fill loop.
        List<long> ids = boardIds.ToList();

        // Group non-deleted comments by board ID and count them in a single database round-trip.
        Dictionary<long, int> counts = await context.BoardComments
            .Where(x => ids.Contains(x.BoardId) && !x.Deleted)
            .GroupBy(x => x.BoardId)
            .Select(g => new { BoardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BoardId, x => x.Count);

        // Fill in zero for board IDs that had no comments, so every requested ID is present.
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
        var existing = await context.BoardComments.FindAsync(comment.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(comment));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmBoardComment"/> row to the <see cref="BoardComment"/> domain object,
    /// applying a null-coalescing default for the required Writer field.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="BoardComment"/> domain object.</returns>
    private static BoardComment ToDomain(OrmBoardComment e) => BoardComment.Reconstitute(
        e.Id, e.BoardId, e.Order, e.AvatarImagePath, e.Writer ?? string.Empty,
        e.Content, e.Created, e.Deleted);

    /// <summary>
    /// Maps a <see cref="BoardComment"/> domain object to an ORM <see cref="OrmBoardComment"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="c">The domain object to convert.</param>
    /// <returns>An <see cref="OrmBoardComment"/> entity suitable for EF Core tracking.</returns>
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
