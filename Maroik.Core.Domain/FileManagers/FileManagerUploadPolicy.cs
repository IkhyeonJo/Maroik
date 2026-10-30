using Maroik.Core.Domain.Results;
using Maroik.Core.Domain.Errors;

namespace Maroik.Core.Domain.FileManagers;

/// <summary>
/// The upload rules of the File Manager: which sizes one file may have, the size of each chunk, how long the page waits for the virus
/// scan, when an unfinished upload is abandoned and whether an upload fits the account's quota.
/// </summary>
public static class FileManagerUploadPolicy
{
    /// <summary>
    /// The largest file ClamAV can scan, 2,147,483,645 bytes: ClamAV cannot scan files of 2 GB or more ("Technical design
    /// limitations prevent ClamAV from scanning files greater than 2 GB", clamd.conf.sample), and clamd clamps the compose files'
    /// 2048M stream limit to this. It is not an upload limit (since 2026-10-25, owner's request, there is none): a larger file can
    /// only be stored without a scan, so <see cref="CheckSize"/> refuses it up front from an account whose virus scan is on.
    /// <c>ClamavClient.MaxStreamBytes</c> holds the same number on the scanner's side.
    /// </summary>
    public const long MaxScannableBytes = 2_147_483_645;

    /// <summary>
    /// The size of each upload chunk, 16 MB: the site sits behind Cloudflare, which caps one request body at 100 MB, and a chunk
    /// stays well inside Kestrel's default 30 MB request limit.
    /// </summary>
    public const int ChunkBytes = 16 * 1024 * 1024;

    /// <summary>
    /// How often, in milliseconds, the page asks whether a completed upload's virus scan is done. The scan runs in the background in
    /// file storage, so every ask is a short request: none waits for ClamAV, which takes minutes over a large archive.
    /// </summary>
    public const int ScanPollMilliseconds = 3_000;

    /// <summary>
    /// How long, in milliseconds, the page waits for a virus scan before it gives the upload up: file storage ends a scan after 15
    /// minutes (<c>FileManagerStorageService.ScanTimeout</c>), and a minute more covers the scans queued before it.
    /// </summary>
    public const int MaxScanWaitMilliseconds = 16 * 60 * 1_000;

    /// <summary>
    /// How long an unfinished upload may go without activity — its start, a chunk, a poll of its virus scan — before it is abandoned
    /// and removed by the worker's hourly sweep or by the same user's next upload. It counts from the last activity, not from the
    /// start, so an upload that takes days but keeps sending is never removed: the page sends a chunk every few minutes at most and asks
    /// about the scan every <see cref="ScanPollMilliseconds"/>, so an hour of silence means the page is gone (closed, offline, asleep) —
    /// and the page cannot resume an upload it lost anyway.
    /// </summary>
    public static readonly TimeSpan StaleUploadAge = TimeSpan.FromHours(1);

    /// <summary>The storage directory every File Manager file lives in, under its GUID name.</summary>
    public const string StorageDirectory = "upload/FileManager";

    /// <summary>Bytes in one GB, the unit the quota is shown and refused in.</summary>
    private const decimal BytesPerGb = 1024m * 1024 * 1024;

    /// <summary>
    /// Accepts a declared file size of 0 bytes or more. One larger than <see cref="MaxScannableBytes"/> can never be scanned, so it is
    /// accepted only when <paramref name="scanDisabled"/> (<c>Account.FileManagerScanDisabled</c>, stored without a scan): an account
    /// whose scan is on would send the whole file only to have it refused at the end.
    /// </summary>
    public static ErrorOr<Success> CheckSize(long size, bool scanDisabled)
    {
        if (size < 0)
            return DomainError.Validation("FileManager.SizeInvalid", "The file size is not valid.");

        if (size > MaxScannableBytes && !scanDisabled)
            return DomainError.Validation("FileManager.FileTooLarge", "A file larger than 2 GB cannot be uploaded.");

        return Result.Success;
    }

    /// <summary>
    /// Accepts an upload of <paramref name="requestedBytes"/> when it fits what is left of <paramref name="quotaBytes"/> after
    /// <paramref name="usedBytes"/> (the user's files, unfinished uploads included). Otherwise the refusal names the shortfall in
    /// GB, rounded up to 0.1 GB, so the user is told "N GB short". A quota already below the usage refuses every new upload.
    /// </summary>
    public static ErrorOr<Success> CheckQuota(long quotaBytes, long usedBytes, long requestedBytes)
    {
        long shortfall = usedBytes + requestedBytes - quotaBytes;
        if (shortfall <= 0)
            return Result.Success;

        decimal shortfallGb = Math.Ceiling(shortfall / BytesPerGb * 10) / 10;
        return DomainError.Validation("FileManager.QuotaExceeded", "Not enough storage: {0} GB short.", shortfallGb);
    }

    /// <summary>Whether an upload last active at <paramref name="lastActivity"/> has been idle for longer than <see cref="StaleUploadAge"/>.</summary>
    public static bool IsStale(DateTime lastActivity, DateTime utcNow) => utcNow - lastActivity > StaleUploadAge;

    /// <summary>
    /// The quota in bytes for one typed in GB by an administrator: 1 GB = 1024³ bytes, rounded to a whole byte. A negative quota,
    /// or one beyond what the <c>bigint</c> column holds, is refused.
    /// </summary>
    public static ErrorOr<long> QuotaBytesFromGb(decimal gb)
    {
        if (gb < 0)
            return DomainError.Validation("Account.FileManagerQuotaNegative", "The storage quota cannot be negative.");

        decimal bytes = Math.Round(gb * BytesPerGb, MidpointRounding.AwayFromZero);
        if (bytes > long.MaxValue)
            return DomainError.Validation("FileManager.QuotaTooLarge", "The storage quota is too large.");

        return (long)bytes;
    }

    /// <summary>The quota in GB, to two decimal places, as the administrator's edit form shows it.</summary>
    public static decimal QuotaGbFromBytes(long bytes) => Math.Round(bytes / BytesPerGb, 2);
}
