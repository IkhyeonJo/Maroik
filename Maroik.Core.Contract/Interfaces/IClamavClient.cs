namespace Maroik.Core.Contract.Interfaces;

public interface IClamavClient
{
    public Task<bool> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort);
}