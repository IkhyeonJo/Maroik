using Microsoft.AspNetCore.Http;

namespace Maroik.Core.Client.Contracts;

public interface IFileClient
{
    public Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl);
    public Task<bool> UploadAsync(IFormFile formFile, string filePath, string fileStorageBaseUrl);
}