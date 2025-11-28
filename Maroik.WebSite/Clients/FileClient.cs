using Maroik.WebSite.Contracts.Clients;
using System.Net;

namespace Maroik.WebSite.Clients;

public class FileClient : IFileClient
{
    private readonly string _fileStorageBaseUrl;

    public FileClient(IConfiguration configuration)
    {
        _fileStorageBaseUrl = configuration["FileStorage:BaseUrl"]?.TrimEnd('/') ?? throw new ArgumentNullException("FileStorage:BaseUrl");
    }

    public async Task<byte[]> DownloadAsync(string filePath)
    {
        using HttpClient httpClient = new();
        using MultipartFormDataContent content = [];
        content.Add(new StringContent(filePath), "filePath");

        HttpResponseMessage response = await httpClient.PostAsync($"{_fileStorageBaseUrl}/api/File/download", content);

        _ = response.EnsureSuccessStatusCode(); // Check success status
        return await response.Content.ReadAsByteArrayAsync(); // Return file data
    }

    public async Task<bool> UploadAsync(IFormFile formFile, string filePath)
    {
        try
        {
            // 1. Safely process file name to prevent path traversal vulnerability
            string sanitizedFileName = Path.GetFileName(filePath); // Remove directory path from user-inputted file name

            // 2. Save file to memory stream and upload to external server
            if (formFile != null && formFile.Length > 0)
            {
                using MemoryStream memoryStream = new();
                // Copy IFormFile data to memory stream
                await formFile.CopyToAsync(memoryStream);
                memoryStream.Position = 0; // Return to start position of stream

                // Upload file to external file server using HttpClient
                using HttpClient httpClient = new();
                using MultipartFormDataContent content = [];

                // Convert memory stream to ByteArrayContent for upload preparation
                StreamContent fileContent = new(memoryStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(formFile.ContentType);

                // Add file to MultipartFormDataContent
                content.Add(fileContent, "file", sanitizedFileName);

                // Add additional parameters
                content.Add(new StringContent(Path.GetDirectoryName(filePath ?? "")), "filePath");

                HttpResponseMessage response = await httpClient.PostAsync($"{_fileStorageBaseUrl}/api/File/upload", content);
                _ = response.EnsureSuccessStatusCode(); // Check status code

                return response.StatusCode == HttpStatusCode.OK;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }
}