namespace Maroik.Core.Client.Contracts;

public interface IFileClient
{
    public Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl);
    public Task<bool> UploadAsync(byte[] fileData, string contentType, string filePath, string fileStorageBaseUrl);
}