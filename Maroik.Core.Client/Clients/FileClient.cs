using System.Diagnostics;
using System.Net;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IFileClient"/> that communicates with the
/// Maroik.FileStorage microservice via HTTP multipart/form-data requests.
/// </summary>
public class FileClient(IHttpClientFactory httpClientFactory, ILogger<FileClient> logger) : IFileClient
{
    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl, CancellationToken ct = default)
    {
        var correlationId = Activity.Current?.Id ?? "";
#pragma warning disable CA1873
        logger.LogInformation("Requesting file download: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
#pragma warning restore CA1873

        using HttpClient httpClient = httpClientFactory.CreateClient();
        using MultipartFormDataContent content = [];

        // Pass the server-side file path to the storage service
        content.Add(new StringContent(filePath), "filePath");
        // So Maroik.FileStorage's failure logs for this call can be traced back to this request.
        content.Add(new StringContent(correlationId), "correlationId");

        HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/download", content, ct);

        _ = response.EnsureSuccessStatusCode(); // Throw on non-2xx status
        return await response.Content.ReadAsByteArrayAsync(ct); // Return raw file bytes
    }

    /// <inheritdoc />
    public async Task<bool> UploadAsync(byte[]? fileData, string contentType, string filePath, string fileStorageBaseUrl, CancellationToken ct = default)
    {
        try
        {
            // Strip any directory component from the file name to prevent path-traversal attacks
            string sanitizedFileName = Path.GetFileName(filePath);

            // Reject empty uploads immediately
            if (fileData is not { Length: > 0 })
            {
                return false;
            }

            var correlationId = Activity.Current?.Id ?? "";
#pragma warning disable CA1873
            logger.LogInformation("Requesting file upload: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
#pragma warning restore CA1873

            using HttpClient httpClient = httpClientFactory.CreateClient();
            using MultipartFormDataContent content = [];

            // Wrap raw bytes with the correct MIME type
            ByteArrayContent fileContent = new(fileData);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            // Attach the file bytes under the "file" field name
            content.Add(fileContent, "file", sanitizedFileName);

            // Tell the storage service where to save the file (directory path only)
            content.Add(new StringContent(Path.GetDirectoryName(filePath) ?? ""), "filePath");
            // So Maroik.FileStorage's failure logs for this call can be traced back to this request.
            content.Add(new StringContent(correlationId), "correlationId");

            HttpResponseMessage response = await httpClient.PostAsync($"{fileStorageBaseUrl}/api/File/upload", content, ct);
            _ = response.EnsureSuccessStatusCode();

            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File upload request failed for {FilePath}", filePath);
            return false;
        }
    }
}
