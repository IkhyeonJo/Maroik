using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Repositories;

public class BoardRepository(ApplicationDbContext context)
    : GenericRepository<Board, OrmBoard>(context), IBoardRepository
{
    protected override DbSet<OrmBoard> Set => Context.Boards;

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id)
        => await Context.Boards
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.View, b => b.View + 1));

    /// <inheritdoc />
    public async Task<long> WriteBoardAsync(Board board)
    {
        OrmBoard entity = ToEntity(board);
        _ = await Context.Boards.AddAsync(entity);
        _ = await Context.SaveChangesAsync();
        return entity.Id;
    }

    protected override Board ToDomain(OrmBoard e) => Board.Reconstitute(
        e.Id, e.Type ?? string.Empty, e.Title ?? string.Empty, e.Content,
        e.Writer ?? string.Empty, e.Created, e.Updated, e.View, e.Deleted, e.Locked, e.Noticed);

    protected override OrmBoard ToEntity(Board b) => new()
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
