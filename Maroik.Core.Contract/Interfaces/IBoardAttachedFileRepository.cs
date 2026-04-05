using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardAttachedFileRepository
{
    public Task<List<BoardAttachedFileResponse>> GetAllAsync();
    public Task<BoardAttachedFileResponse?> GetByBoardIdAsync(long boardId);
    public Task<Dictionary<long, BoardAttachedFileResponse?>> GetByBoardIdsAsync(IEnumerable<long> boardIds);
    public Task SaveBoardAttachedFileAsync(BoardAttachedFileRequest request);
    public Task UpdateBoardAttachedFileAsync(BoardAttachedFileRequest request);
}
