namespace Maroik.WebSite.Contracts
{
    public interface IFileRepository
    {
        public Task<byte[]> DownloadFileAsync(string fileName);
        public Task UploadFileAsync(string fileName, byte[] fileData);
        public Task DeleteFileAsync(string fileName);
    }
}