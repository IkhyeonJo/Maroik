using Maroik.Core.Contract.Domains.Board;
namespace Maroik.Core.Contract.Interfaces;
public interface IBoardAttachedFileRepository : IGenericRepository<BoardAttachedFile>
{
    Task<Dictionary<long, BoardAttachedFile?>> GetByBoardIdsAsync(IEnumerable<long> boardIds, CancellationToken ct = default);
    Task<BoardAttachedFile?> FindByBoardIdAsync(long boardId, CancellationToken ct = default);
}
