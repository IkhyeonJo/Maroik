namespace Maroik.FileStorage.Contracts
{
    public interface IClamavClient
    {
        public Task<bool> ScanWithClamavAsync(Stream fileStream);
    }
}