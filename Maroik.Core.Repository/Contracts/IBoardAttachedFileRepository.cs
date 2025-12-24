using Maroik.Core.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IBoardAttachedFileRepository
{
    /// <summary>
    /// Gets all registered files.
    /// </summary>
    /// <returns></returns>
    public Task<List<BoardAttachedFile>> GetAllAsync();

    /// <summary>
    /// Saves an attached file.
    /// </summary>
    /// <param name="boardAttachedFile"></param>
    /// <returns></returns>
    public Task SaveBoardAttachedFileAsync(BoardAttachedFile boardAttachedFile);

    /// <summary>
    /// Updates an attached file.
    /// </summary>
    /// <param name="boardAttachedFile"></param>
    /// <returns></returns>
    public Task UpdateBoardAttachedFileAsync(BoardAttachedFile boardAttachedFile);
}