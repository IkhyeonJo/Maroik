namespace Maroik.WebSite.Contracts
{
    public interface IClamavClient
    {
        public Task<bool> ScanWithClamavAsync(Stream fileStream);
    }
}