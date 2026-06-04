namespace Maroik.Core.Contract.Interfaces;

public interface IGenericRepository<TDomain>
{
    Task CreateAsync(TDomain domain, CancellationToken ct = default);
    Task UpdateEntityAsync(TDomain domain, CancellationToken ct = default);
}
