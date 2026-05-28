using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.BoardAttachedFile;

namespace Maroik.Core.Repository.Repositories;

public class BoardAttachedFileRepository(ApplicationDbContext context)
    : GenericRepository<BoardAttachedFile, OrmFile>(context), IBoardAttachedFileRepository
{
    protected override DbSet<OrmFile> Set => Context.BoardAttachedFiles;

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();
        List<OrmFile> files = await Context.BoardAttachedFiles
            .Where(x => ids.Contains(x.BoardId))
            .ToListAsync();

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
}
