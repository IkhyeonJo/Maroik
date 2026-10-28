using Maroik.Core.Domain.Errors;
using Maroik.Core.Domain.FileManagers;
namespace Maroik.Core.Domain.Tests.FileManagers;

/// <summary>
/// Unit tests for <see cref="FileManagerUploadPolicy"/>: the largest file ClamAV can scan, the chunk size, the age of
/// an abandoned upload's idle time, how long the page waits for the virus scan and the quota check.
/// </summary>
public class FileManagerUploadPolicyTests
{
    /// <summary>
    /// The fixed limits: ClamAV scans a file of at most 2,147,483,645 bytes (the stream limit clamd clamps 2048M to), 16 MB chunks, and
    /// an upload abandoned after an hour without activity.
    /// </summary>
    [Fact]
    public void TheLimits_AreTheAgreedValues()
    {
        Assert.Equal(2_147_483_645L, FileManagerUploadPolicy.MaxScannableBytes);
        Assert.Equal(16 * 1024 * 1024, FileManagerUploadPolicy.ChunkBytes);
        Assert.Equal(TimeSpan.FromHours(1), FileManagerUploadPolicy.StaleUploadAge);
    }

    /// <summary>
    /// While a file is scanned the page asks every 3 seconds, and gives up after 16 minutes: the scan's own limit in file storage (15
    /// minutes) plus a minute for the queue.
    /// </summary>
    [Fact]
    public void TheScanWait_IsAskedEveryThreeSeconds_ForAtMostSixteenMinutes()
    {
        Assert.Equal(3_000, FileManagerUploadPolicy.ScanPollMilliseconds);
        Assert.Equal(16 * 60 * 1_000, FileManagerUploadPolicy.MaxScanWaitMilliseconds);
    }

    /// <summary>An empty file and a file ClamAV can still scan are accepted from every account.</summary>
    [Theory]
    [InlineData(0L, false)]
    [InlineData(1L, false)]
    [InlineData(2_147_483_645L, false)]
    [InlineData(2_147_483_645L, true)]
    public void CheckSize_AcceptsAScannableFile(long size, bool scanDisabled)
    {
        Assert.False(FileManagerUploadPolicy.CheckSize(size, scanDisabled).IsError);
    }

    /// <summary>
    /// There is no upper limit: a file larger than ClamAV can scan is accepted from an account whose virus scan an administrator
    /// turned off (it is stored without a scan), whatever its size.
    /// </summary>
    [Theory]
    [InlineData(2_147_483_646L)]
    [InlineData(50L * 1024 * 1024 * 1024)]
    [InlineData(long.MaxValue)]
    public void CheckSize_AcceptsAnUnscannableFile_FromAnAccountWhoseScanIsOff(long size)
    {
        Assert.False(FileManagerUploadPolicy.CheckSize(size, scanDisabled: true).IsError);
    }

    /// <summary>
    /// From an account whose scan is on, a file one byte over what ClamAV can scan is refused when the upload starts (it could never be scanned,
    /// so it would be refused after the whole file was sent); a negative size is refused from every account.
    /// </summary>
    [Theory]
    [InlineData(2_147_483_646L, false, "FileManager.FileTooLarge")]
    [InlineData(-1L, false, "FileManager.SizeInvalid")]
    [InlineData(-1L, true, "FileManager.SizeInvalid")]
    public void CheckSize_RefusesAFileThatCannotBeScanned_OrANegativeSize(long size, bool scanDisabled, string code)
    {
        Assert.Equal(code, FileManagerUploadPolicy.CheckSize(size, scanDisabled).FirstError.Code);
    }

    /// <summary>An upload that fits the remaining quota exactly is accepted.</summary>
    [Fact]
    public void CheckQuota_AcceptsAnUploadThatFitsExactly()
    {
        Assert.False(FileManagerUploadPolicy.CheckQuota(quotaBytes: 100, usedBytes: 60, requestedBytes: 40).IsError);
    }

    /// <summary>An upload over the quota is refused, saying how many GB are missing (rounded up to 0.1 GB).</summary>
    [Fact]
    public void CheckQuota_RefusesAnUploadOverTheQuota_NamingTheShortfallInGb()
    {
        const long gb = 1024L * 1024 * 1024;

        var result = FileManagerUploadPolicy.CheckQuota(quotaBytes: 30 * gb, usedBytes: 29 * gb, requestedBytes: 2 * gb + 1);

        Assert.Equal("FileManager.QuotaExceeded", result.FirstError.Code);
        Assert.Equal([1.1m], (object[])result.FirstError.Metadata![DomainError.MessageArgsMetadataKey]);
    }

    /// <summary>With the default quota of 0 GB every non-empty upload is refused; an empty file still fits.</summary>
    [Fact]
    public void CheckQuota_UnderTheDefaultZeroQuota_RefusesEveryNonEmptyFile()
    {
        Assert.Equal("FileManager.QuotaExceeded", FileManagerUploadPolicy.CheckQuota(0, 0, 1).FirstError.Code);
        Assert.Equal([0.1m], (object[])FileManagerUploadPolicy.CheckQuota(0, 0, 1).FirstError.Metadata![DomainError.MessageArgsMetadataKey]);
        Assert.False(FileManagerUploadPolicy.CheckQuota(0, 0, 0).IsError);
    }

    /// <summary>A quota already below the usage (an administrator lowered it) blocks new uploads but nothing else is implied.</summary>
    [Fact]
    public void CheckQuota_RefusesAnUpload_WhenTheUsageIsAlreadyOverTheQuota()
    {
        Assert.Equal("FileManager.QuotaExceeded", FileManagerUploadPolicy.CheckQuota(quotaBytes: 10, usedBytes: 50, requestedBytes: 1).FirstError.Code);
    }

    /// <summary>An upload is stale once its last activity is more than an hour ago.</summary>
    [Fact]
    public void IsStale_IsTrue_OnlyPastAnHourWithoutActivity()
    {
        DateTime now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

        Assert.False(FileManagerUploadPolicy.IsStale(now.AddHours(-1), now));
        Assert.True(FileManagerUploadPolicy.IsStale(now.AddHours(-1).AddSeconds(-1), now));
    }

    // -- Quota units (the administrator enters GB) -----------------------------------

    /// <summary>A quota typed in GB becomes bytes (1 GB = 1024³ bytes), fractions included and rounded to a whole byte.</summary>
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("30", 32_212_254_720L)]
    [InlineData("0.5", 536_870_912L)]
    public void QuotaBytesFromGb_ConvertsGbToBytes(string gb, long bytes)
    {
        Assert.Equal(bytes, FileManagerUploadPolicy.QuotaBytesFromGb(decimal.Parse(gb, System.Globalization.CultureInfo.InvariantCulture)).Value);
    }

    /// <summary>A negative quota, or one too large to store, is refused.</summary>
    [Theory]
    [InlineData("-0.1", "Account.FileManagerQuotaNegative")]
    [InlineData("10000000000", "FileManager.QuotaTooLarge")]
    public void QuotaBytesFromGb_RefusesANegativeOrHugeQuota(string gb, string code)
    {
        Assert.Equal(code, FileManagerUploadPolicy.QuotaBytesFromGb(decimal.Parse(gb, System.Globalization.CultureInfo.InvariantCulture)).FirstError.Code);
    }

    /// <summary>Bytes shown back in GB keep up to two decimal places (what the administrator typed comes back).</summary>
    [Theory]
    [InlineData(0L, "0")]
    [InlineData(32_212_254_720L, "30")]
    [InlineData(536_870_912L, "0.5")]
    [InlineData(1L, "0")]
    public void QuotaGbFromBytes_ConvertsBytesToGb(long bytes, string gb)
    {
        Assert.Equal(decimal.Parse(gb, System.Globalization.CultureInfo.InvariantCulture), FileManagerUploadPolicy.QuotaGbFromBytes(bytes));
    }
}
