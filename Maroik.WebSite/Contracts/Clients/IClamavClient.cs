namespace Maroik.WebSite.Contracts.Clients;

public interface IClamavClient
{
    public Task<bool> ScanWithClamavAsync(Stream fileStream);
}