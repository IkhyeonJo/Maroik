using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.BoardAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="BoardAttachedFile"/> domain objects.
/// Provides a batched lookup that retrieves one representative file per board ID.
/// </summary>
public class BoardAttachedFileRepository(ApplicationDbContext context)
    : GenericRepository<BoardAttachedFile, OrmFile>(context), IBoardAttachedFileRepository
{
    protected override DbSet<OrmFile> Set => Context.BoardAttachedFiles;

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct = default)
    {
        List<long> ids = boardIds.ToList();
        List<OrmFile> files = await Context.BoardAttachedFiles
            .Where(x => ids.Contains(x.BoardId))
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => (BoardAttachedFile?)(files.FirstOrDefault(f => f.BoardId == id) is { } f ? ToDomain(f) : null));
    }

    protected override BoardAttachedFile ToDomain(OrmFile e)
        => BoardAttachedFile.Reconstitute(e.Id, e.BoardId, e.Size, e.Name, e.Extension, e.Path);

    protected override OrmFile ToEntity(BoardAttachedFile f) => new()
    {
        Id = f.Id,
        BoardId = f.BoardId,
        Size = f.Size,
        Name = f.Name,
        Extension = f.Extension,
        Path = f.Path
    };

    public Task<BoardAttachedFile?> FindByBoardIdAsync(long boardId, CancellationToken ct = default)
        => QueryFirstAsync(e => e.BoardId == boardId, ct);
}
