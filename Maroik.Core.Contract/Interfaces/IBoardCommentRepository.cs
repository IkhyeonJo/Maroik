using Maroik.Core.Contract.Domains.Board;
namespace Maroik.Core.Contract.Interfaces;
public interface IBoardCommentRepository : IGenericRepository<BoardComment>
{
    Task<Dictionary<long, int>> GetCommentCountsByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct = default);
    Task<List<BoardComment>> GetByBoardIdOrderedAsync(long boardId, CancellationToken ct = default);
    Task<BoardComment?> FindByIdAsync(long id, CancellationToken ct = default);
}
