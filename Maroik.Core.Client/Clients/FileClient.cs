using Maroik.Core.Contract.Interfaces;
using System.Net;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IFileClient"/> that communicates with the
/// Maroik.FileStorage microservice via HTTP multipart/form-data requests.
/// </summary>
public class FileClient(IHttpClientFactory httpClientFactory) : IFileClient
{
    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl, CancellationToken ct = default)
    {
        using HttpClient httpClient = httpClientFactory.CreateClient();
        using MultipartFormDataContent content = [];

        // Pass the server-side file path to the storage service
        content.Add(new StringContent(filePath), "filePath");

        HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/download", content, ct);

        _ = response.EnsureSuccessStatusCode(); // Throw on non-2xx status
        return await response.Content.ReadAsByteArrayAsync(ct); // Return raw file bytes
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(byte[] fileData, string contentType, string filePath, string fileStorageBaseUrl, CancellationToken ct = default)
    {
        try
        {
            // Strip any directory component from the file name to prevent path-traversal attacks
            string sanitizedFileName = Path.GetFileName(filePath);

            // Reject empty uploads immediately
            if (fileData != null && fileData.Length > 0)
            {
                using HttpClient httpClient = httpClientFactory.CreateClient();
                using MultipartFormDataContent content = [];

                // Wrap raw bytes with the correct MIME type
                ByteArrayContent fileContent = new(fileData);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

                // Attach the file bytes under the "file" field name
                content.Add(fileContent, "file", sanitizedFileName);

                // Tell the storage service where to save the file (directory path only)
                content.Add(new StringContent(Path.GetDirectoryName(filePath ?? "") ?? ""), "filePath");

                HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/upload", content, ct);
                _ = response.EnsureSuccessStatusCode();

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
