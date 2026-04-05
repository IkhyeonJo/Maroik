namespace Maroik.Core.Contract.Dtos;

public class AttachedFileInfo
{
    public byte[] Bytes { get; init; } = [];
    public string ContentType { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long Size { get; init; }
}
