using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The upload rules the Website needs; each member answers from a Domain policy.</summary>
public interface IUploadPolicyService
{
    /// <summary>Whether an uploaded image may have this extension.</summary>
    bool IsAllowedImageExtension(string? extension);

    /// <summary>The content types an uploaded image may have.</summary>
    IReadOnlyList<string> GetAllowedImageContentTypes();

    /// <summary>Turns a File Manager quota typed in GB into bytes, or refuses a negative or oversized value.</summary>
    ServiceValueResult<long> FileManagerQuotaBytesFromGb(decimal gb);

    /// <summary>A File Manager quota in GB, for the admin form.</summary>
    decimal FileManagerQuotaGbFromBytes(long bytes);

    /// <summary>The upload timings the File Manager page follows.</summary>
    FileManagerFormRules GetFileManagerFormRules();
}
