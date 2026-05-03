using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.BoardAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IBoardAttachedFileRepository"/>.</summary>
public class BoardAttachedFileRepository(ApplicationDbContext context) : IBoardAttachedFileRepository
{
    /// <inheritdoc />
    public async Task<List<BoardAttachedFile>> GetAllAsync()
        => (await context.BoardAttachedFiles.ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<BoardAttachedFile?> GetByBoardIdAsync(long boardId)
    {
        OrmFile? file = await context.BoardAttachedFiles.FirstOrDefaultAsync(x => x.BoardId == boardId);
        return file == null ? null : ToDomain(file);
    }

    /// <inheritdoc />
    public async Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds)
    {
        List<long> ids = boardIds.ToList();
        List<OrmFile> files = await context.BoardAttachedFiles
            .Where(x => ids.Contains(x.BoardId))
            .ToListAsync();
        return ids.ToDictionary(
            id => id,
            id => (BoardAttachedFile?)(files.FirstOrDefault(f => f.BoardId == id) is { } f ? ToDomain(f) : null));
    }

    /// <inheritdoc />
    public async Task SaveBoardAttachedFileAsync(BoardAttachedFile file)
    {
        _ = await context.BoardAttachedFiles.AddAsync(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateBoardAttachedFileAsync(BoardAttachedFile file)
    {
        _ = context.BoardAttachedFiles.Update(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    private static BoardAttachedFile ToDomain(OrmFile e)
        => BoardAttachedFile.Reconstitute(e.Id, e.BoardId, e.Size, e.Name, e.Extension, e.Path);

    private static OrmFile ToEntity(BoardAttachedFile f) => new()
    {
        Id = f.Id,
        BoardId = f.BoardId,
        Size = f.Size,
        Name = f.Name,
        Extension = f.Extension,
        Path = f.Path
    };
}
