namespace Maroik.Core.Contract.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
