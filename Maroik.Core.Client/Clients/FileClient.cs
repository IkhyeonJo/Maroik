using System.Diagnostics;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IFileClient"/> that communicates with the
/// Maroik.FileStorage microservice via HTTP multipart/form-data requests.
/// </summary>
public class FileClient(IHttpClientFactory httpClientFactory, ILogger<FileClient> logger) : IFileClient
{
    /// <summary>
    /// Rejects a storage path that is rooted or contains a <c>..</c> segment, so a value that ever
    /// reaches here from a less-trusted source can't be used to escape the storage root.
    /// The app's own paths are always relative (e.g. <c>upload/Forum/.../file.zip</c>).
    /// </summary>
    private static bool IsSafeRelativePath(string filePath) =>
        !string.IsNullOrWhiteSpace(filePath)
        && !Path.IsPathRooted(filePath)
        && !filePath.Replace('\\', '/').Split('/').Contains("..");

    /// <inheritdoc />
    public async Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl, CancellationToken ct = default)
    {
        if (!IsSafeRelativePath(filePath))
            throw new ArgumentException($"Unsafe storage path: '{filePath}'.", nameof(filePath));

        var correlationId = Activity.Current?.Id ?? "";
#pragma warning disable CA1873
        logger.LogInformation("Requesting file download: {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
#pragma warning restore CA1873

        try
        {
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
        catch (Exception ex)
        {
            // Parity with UploadAsync: log with the correlation id, then rethrow (callers of the
            // download path already handle the exception and this method has no "false" to return).
            logger.LogError(ex, "File download request failed for {FilePath}. CorrelationId={CorrelationId}", filePath, correlationId);
            throw;
        }
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

            // The directory portion is forwarded to the storage service as-is; reject a rooted or
            // ".."-containing path so it can't place the file outside the storage root.
            if (!IsSafeRelativePath(filePath))
            {
                logger.LogWarning("Rejected file upload with unsafe storage path: {FilePath}", filePath);
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

            // Any 2xx means the storage service accepted the file — don't treat a 201/204 as a
            // failure (which would make the caller roll back over a file that was actually stored).
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("File upload returned {StatusCode} for {FilePath}", (int)response.StatusCode, filePath);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File upload request failed for {FilePath}", filePath);
            return false;
        }
    }
}
