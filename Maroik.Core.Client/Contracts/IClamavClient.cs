namespace Maroik.Core.Client.Contracts;

public interface IClamavClient
{
    public Task<bool> ScanWithClamavAsync(Stream fileStream, string clamavHost, int clamavPort);
}