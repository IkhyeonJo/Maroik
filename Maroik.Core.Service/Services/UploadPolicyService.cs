using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IUploadPolicyService"/>
public sealed class UploadPolicyService : IUploadPolicyService
{
    /// <inheritdoc />
    public bool IsAllowedImageExtension(string? extension) => ImageUploadPolicy.IsAllowedExtension(extension);

    /// <inheritdoc />
    public IReadOnlyList<string> GetAllowedImageContentTypes() => ImageUploadPolicy.AllowedContentTypes;

    /// <inheritdoc />
    public ServiceValueResult<long> FileManagerQuotaBytesFromGb(decimal gb)
    {
        var quota = FileManagerUploadPolicy.QuotaBytesFromGb(gb);
        if (quota.IsError) return ServiceResult.FromError(quota.FirstError);
        return quota.Value;
    }

    /// <inheritdoc />
    public decimal FileManagerQuotaGbFromBytes(long bytes) => FileManagerUploadPolicy.QuotaGbFromBytes(bytes);

    /// <inheritdoc />
    public FileManagerFormRules GetFileManagerFormRules() => new()
    {
        ScanPollMilliseconds = FileManagerUploadPolicy.ScanPollMilliseconds,
        MaxScanWaitMilliseconds = FileManagerUploadPolicy.MaxScanWaitMilliseconds,
    };
}
