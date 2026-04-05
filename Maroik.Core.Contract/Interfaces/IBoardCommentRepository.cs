using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IBoardCommentRepository
{
    public Task<IEnumerable<BoardCommentResponse>> GetOneBoardCommentsAsync(long boardId);
    public Task<BoardCommentResponse?> GetBoardCommentAsync(int id);
    public Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds);
    public Task WriteBoardCommentAsync(BoardCommentRequest request);
    public Task DeleteBoardCommentAsync(BoardCommentRequest request);
}
