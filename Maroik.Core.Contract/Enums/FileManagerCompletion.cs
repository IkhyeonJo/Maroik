namespace Maroik.Core.Contract.Enums;

/// <summary>Where a File Manager upload stands after <c>IFileManagerService.CompleteUploadAsync</c>.</summary>
public enum FileManagerCompletion
{
    /// <summary>Decided: the file is stored (a success), or refused (the result says why).</summary>
    Decided = 0,

    /// <summary>File storage is still scanning it in the background; nothing changed, asked again later.</summary>
    Scanning
}
