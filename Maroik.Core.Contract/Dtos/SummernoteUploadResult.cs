namespace Maroik.Core.Contract.Dtos;

public class SummernoteUploadResult
{
    public bool Success { get; init; }
    public byte[]? FileBytes { get; init; }
    public string? ContentType { get; init; }
    public string? FileName { get; init; }
    public string? FilePath { get; init; }
    public string? ErrorKey { get; init; }

    public static SummernoteUploadResult Ok(byte[] fileBytes, string contentType, string fileName, string filePath)
        => new() { Success = true, FileBytes = fileBytes, ContentType = contentType, FileName = fileName, FilePath = filePath };

    public static SummernoteUploadResult Fail(string errorKey)
        => new() { Success = false, ErrorKey = errorKey };
}
