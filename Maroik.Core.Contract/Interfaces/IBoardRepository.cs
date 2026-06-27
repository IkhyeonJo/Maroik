using Maroik.Core.Contract.Domains.Board;
namespace Maroik.Core.Contract.Interfaces;

public interface IBoardRepository : IGenericRepository<Board>
{
    Task IncrementViewAsync(long id, CancellationToken ct = default);
    Task<long> WriteBoardAsync(Board board, CancellationToken ct = default);
    Task<List<Board>> GetByTypeAsync(string type, CancellationToken ct = default);
    Task<Board?> FindActiveByIdAsync(long id, CancellationToken ct = default);
}
