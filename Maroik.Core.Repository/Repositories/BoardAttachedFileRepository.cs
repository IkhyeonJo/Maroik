using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.BoardAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBoardAttachedFileRepository"/> that persists and
/// retrieves <see cref="BoardAttachedFile"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // Materialise to a list so we can reference it both in the database query and in the dictionary projection.
        List<long> ids = boardIds.ToList();
        List<OrmFile> files = await context.BoardAttachedFiles
            .Where(x => ids.Contains(x.BoardId))
            .ToListAsync();

        // Build a dictionary keyed by each requested board ID, returning null for boards with no file.
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
        var existing = await context.BoardAttachedFiles.FindAsync(file.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmFile"/> row to the <see cref="BoardAttachedFile"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="BoardAttachedFile"/> domain object.</returns>
    private static BoardAttachedFile ToDomain(OrmFile e)
        => BoardAttachedFile.Reconstitute(e.Id, e.BoardId, e.Size, e.Name, e.Extension, e.Path);

    /// <summary>
    /// Maps a <see cref="BoardAttachedFile"/> domain object to an ORM <see cref="OrmFile"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="f">The domain object to convert.</param>
    /// <returns>An <see cref="OrmFile"/> entity suitable for EF Core tracking.</returns>
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
