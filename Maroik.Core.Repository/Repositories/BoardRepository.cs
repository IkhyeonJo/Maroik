using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBoardRepository"/> that persists and retrieves
/// <see cref="Board"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class BoardRepository(ApplicationDbContext context) : IBoardRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Board>> GetOneTypeBoardsAsync(string type)
        => (await context.Boards.Where(x => x.Type == type).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<Board?> GetBoardByIdAsync(long id)
    {
        // Only return boards that have not been soft-deleted.
        OrmBoard? entity = await context.Boards.FirstOrDefaultAsync(x => x.Id == id && !x.Deleted);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id)
    {
        // Use ExecuteUpdateAsync for a single-column increment without loading the full entity.
        await context.Boards
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.View, b => b.View + 1));
    }

    /// <inheritdoc />
    public async Task<long> WriteBoardAsync(Board board)
    {
        OrmBoard entity = ToEntity(board);
        _ = await context.Boards.AddAsync(entity);
        _ = await context.SaveChangesAsync();
        // The database generates the Id; return it so the caller can attach related records.
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task UpdateBoardAsync(Board board)
    {
        _ = context.Boards.Update(ToEntity(board));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteBoardAsync(Board board)
    {
        // Soft-delete: the domain object sets Deleted = true before calling this method,
        // so we issue an Update rather than a Remove.
        _ = context.Boards.Update(ToEntity(board));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmBoard"/> row to the <see cref="Board"/> domain object,
    /// applying null-coalescing defaults for required string fields.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Board"/> domain object.</returns>
    private static Board ToDomain(OrmBoard e) => Board.Reconstitute(
        e.Id, e.Type ?? string.Empty, e.Title ?? string.Empty, e.Content,
        e.Writer ?? string.Empty, e.Created, e.Updated, e.View, e.Deleted, e.Locked, e.Noticed);

    /// <summary>
    /// Maps a <see cref="Board"/> domain object to an ORM <see cref="OrmBoard"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="b">The domain object to convert.</param>
    /// <returns>An <see cref="OrmBoard"/> entity suitable for EF Core tracking.</returns>
    private static OrmBoard ToEntity(Board b) => new()
    {
        Id = b.Id,
        Type = b.Type,
        Title = b.Title,
        Content = b.Content,
        Writer = b.Writer,
        Created = b.Created,
        Updated = b.Updated,
        View = b.View,
        Deleted = b.Deleted,
        Locked = b.Locked,
        Noticed = b.Noticed
    };
}
