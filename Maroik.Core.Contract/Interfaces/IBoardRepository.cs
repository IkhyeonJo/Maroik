using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardRepository
{
    public Task<IEnumerable<BoardResponse>> GetOneTypeBoardsAsync(string type);
    public Task WriteBoardAsync(BoardRequest request);
    public Task UpdateBoardAsync(BoardRequest request);
    public Task DeleteBoardAsync(BoardRequest request);
}
