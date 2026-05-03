using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IBoardRepository"/>.</summary>
public class BoardRepository(ApplicationDbContext context) : IBoardRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<Board>> GetOneTypeBoardsAsync(string type)
        => (await context.Boards.Where(x => x.Type == type).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<Board?> GetBoardByIdAsync(long id)
    {
        OrmBoard? entity = await context.Boards.FirstOrDefaultAsync(x => x.Id == id && !x.Deleted);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id)
    {
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
        _ = context.Boards.Update(ToEntity(board));
        _ = await context.SaveChangesAsync();
    }

    private static Board ToDomain(OrmBoard e) => Board.Reconstitute(
        e.Id, e.Type ?? string.Empty, e.Title ?? string.Empty, e.Content,
        e.Writer ?? string.Empty, e.Created, e.Updated, e.View, e.Deleted, e.Locked, e.Noticed);

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
