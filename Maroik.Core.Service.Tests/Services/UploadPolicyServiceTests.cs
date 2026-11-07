using Maroik.Core.Contract.Dtos;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Uploads;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="UploadPolicyService"/>: each member answers what the Domain answers.</summary>
public class UploadPolicyServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly UploadPolicyService _sut = new();

    /// <summary>Only the policy's image extensions are allowed.</summary>
    [Theory]
    [InlineData(".png", true)]
    [InlineData(".gif", false)]
    [InlineData(null, false)]
    public void IsAllowedImageExtension_FollowsThePolicy(string? extension, bool expected) =>
        Assert.Equal(expected, _sut.IsAllowedImageExtension(extension));

    /// <summary>The allowed content types are the policy's.</summary>
    [Fact]
    public void GetAllowedImageContentTypes_AreThePolicies() =>
        Assert.Equal(ImageUploadPolicy.AllowedContentTypes, _sut.GetAllowedImageContentTypes());

    /// <summary>A valid quota comes back in bytes with a successful result.</summary>
    [Fact]
    public void FileManagerQuotaBytesFromGb_TurnsGigabytesIntoBytes()
    {
        (ServiceResult result, long bytes) = _sut.FileManagerQuotaBytesFromGb(2m);

        Assert.True(result.Success);
        Assert.Equal(FileManagerUploadPolicy.QuotaBytesFromGb(2m).Value, bytes);
    }

    /// <summary>A negative quota comes back as the policy's refusal, with its message key.</summary>
    [Fact]
    public void FileManagerQuotaBytesFromGb_RefusesANegativeQuota()
    {
        (ServiceResult result, long bytes) = _sut.FileManagerQuotaBytesFromGb(-1m);

        Assert.False(result.Success);
        Assert.Equal(ServiceResult.FromError(FileManagerUploadPolicy.QuotaBytesFromGb(-1m).FirstError).ErrorKey, result.ErrorKey);
        Assert.Equal(0, bytes);
    }

    /// <summary>Bytes are turned back into the admin form's gigabytes.</summary>
    [Fact]
    public void FileManagerQuotaGbFromBytes_TurnsBytesIntoGigabytes() =>
        Assert.Equal(FileManagerUploadPolicy.QuotaGbFromBytes(1_610_612_736), _sut.FileManagerQuotaGbFromBytes(1_610_612_736));

    /// <summary>The File Manager rules carry the upload policy's timings.</summary>
    [Fact]
    public void GetFileManagerFormRules_CarriesThePolicy()
    {
        FileManagerFormRules rules = _sut.GetFileManagerFormRules();

        Assert.Equal(FileManagerUploadPolicy.ScanPollMilliseconds, rules.ScanPollMilliseconds);
        Assert.Equal(FileManagerUploadPolicy.MaxScanWaitMilliseconds, rules.MaxScanWaitMilliseconds);
    }
}
