using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Board"/> domain objects.
/// Provides board-specific operations such as incrementing the view counter and returning the generated ID on write.
/// </summary>
public class BoardRepository(ApplicationDbContext context)
    : GenericRepository<Board, OrmBoard>(context), IBoardRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmBoard"/> rows.</summary>
    protected override DbSet<OrmBoard> Set => Context.Boards;

    /// <inheritdoc />
    public async Task IncrementViewAsync(long id, CancellationToken ct = default)
        => await Context.Boards
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.View, b => b.View + 1), ct);

    /// <inheritdoc />
    public async Task<long> WriteBoardAsync(Board board, CancellationToken ct = default)
    {
        OrmBoard entity = ToEntity(board);
        _ = await Context.Boards.AddAsync(entity, ct);
        _ = await Context.SaveChangesAsync(ct);
        return entity.Id;
    }

    /// <summary>Maps a persisted <see cref="OrmBoard"/> row to the <see cref="Board"/> domain object.</summary>
    protected override Board ToDomain(OrmBoard e) => Board.Reconstitute(
        e.Id, e.Type ?? "", e.Title ?? "", e.Content,
        e.Writer ?? "", e.Created, e.Updated, e.View, e.Deleted, e.Locked, e.Noticed);

    /// <summary>Maps a <see cref="Board"/> domain object to its <see cref="OrmBoard"/> persistence representation.</summary>
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

    /// <inheritdoc />
    public Task<List<Board>> GetByTypeAsync(string type, CancellationToken ct = default)
        => QueryAsync(e => e.Type == type, ct: ct);

    /// <inheritdoc />
    public Task<Board?> FindActiveByIdAsync(long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.Id == id && e.Deleted == false, ct);
}
