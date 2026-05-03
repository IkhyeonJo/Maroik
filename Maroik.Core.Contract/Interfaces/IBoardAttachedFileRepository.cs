using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the BoardAttachedFile table.
/// Each board post may have at most one attached file.
/// </summary>
public interface IBoardAttachedFileRepository
{
    /// <summary>Returns all attached-file records (admin / migration use).</summary>
    public Task<List<BoardAttachedFile>> GetAllAsync();

    /// <summary>Returns the attached file for the given board post, or null if none exists.</summary>
    public Task<BoardAttachedFile?> GetByBoardIdAsync(long boardId);

    /// <summary>Batch-loads attached files for multiple board IDs; value is null when no file is attached.</summary>
    public Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds);

    /// <summary>Inserts a new attached-file record.</summary>
    public Task SaveBoardAttachedFileAsync(BoardAttachedFile file);

    /// <summary>Updates the metadata (name, path, size, extension) of an existing attached-file record.</summary>
    public Task UpdateBoardAttachedFileAsync(BoardAttachedFile file);
}
