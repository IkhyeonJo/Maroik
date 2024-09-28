using Maroik.WebSite.Contracts;

public class FileRepository : IFileRepository
{
    private readonly HttpClient _httpClient;
    private readonly string _fileStorageDomain = "Maroik.FileStorage";

    public FileRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]> DownloadFileAsync(string fileName)
    {
        var response = await _httpClient.GetAsync($"https://{_fileStorageDomain}/files/{fileName}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task UploadFileAsync(string fileName, byte[] fileData)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileData), "file", fileName);

        var response = await _httpClient.PostAsync($"https://{_fileStorageDomain}/files", content);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var response = await _httpClient.DeleteAsync($"https://{_fileStorageDomain}/files/{fileName}");
        response.EnsureSuccessStatusCode();
    }
}
