namespace Maroik.WebSite.Contracts.Clients
{
    public interface IFileClient
    {
        public Task<byte[]> DownloadAsync(string filePath);
        public Task<bool> UploadAsync(IFormFile formFile, string filePath);
    }
}