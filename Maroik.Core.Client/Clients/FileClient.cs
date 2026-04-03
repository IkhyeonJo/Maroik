using Maroik.Core.Client.Contracts;
using System.Net;

namespace Maroik.Core.Client.Clients;

public class FileClient : IFileClient
{
    public async Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl)
    {
        using HttpClient httpClient = new();
        using MultipartFormDataContent content = [];
        content.Add(new StringContent(filePath), "filePath");

        HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/download", content);

        _ = response.EnsureSuccessStatusCode(); // Check success status
        return await response.Content.ReadAsByteArrayAsync(); // Return file data
    }

    public async Task<bool> UploadAsync(byte[] fileData, string contentType, string filePath, string fileStorageBaseUrl)
    {
        try
        {
            // 1. Safely process file name to prevent path traversal vulnerability
            string sanitizedFileName = Path.GetFileName(filePath); // Remove directory path from user-inputted file name

            // 2. Upload file data to external server
            if (fileData != null && fileData.Length > 0)
            {
                // Upload file to external file server using HttpClient
                using HttpClient httpClient = new();
                using MultipartFormDataContent content = [];

                ByteArrayContent fileContent = new(fileData);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                // Add file to MultipartFormDataContent
                content.Add(fileContent, "file", sanitizedFileName);

                // Add additional parameters
                content.Add(new StringContent(Path.GetDirectoryName(filePath ?? "") ?? ""), "filePath");

                HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/upload", content);
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