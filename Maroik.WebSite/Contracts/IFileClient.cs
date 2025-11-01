namespace Maroik.WebSite.Contracts
{
    public interface IFileClient
    {
        public Task<byte[]> DownloadAsync(string filePath);
        public Task<bool> UploadAsync(IFormFile formFile, string filePath);
    }
}