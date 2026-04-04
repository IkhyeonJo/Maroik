using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardAttachedFileRepository
{
    public Task<List<BoardAttachedFileResponse>> GetAllAsync();
    public Task SaveBoardAttachedFileAsync(BoardAttachedFileRequest request);
    public Task UpdateBoardAttachedFileAsync(BoardAttachedFileRequest request);
}
