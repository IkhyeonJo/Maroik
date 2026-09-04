namespace Maroik.FileStorage.Settings;

/// <summary>
/// Strongly-typed configuration bound to the "ServerSetting" section of appsettings.json.
/// Deliberately reuses the same section/env-var name as Maroik.Website's
/// <c>Maroik.Core.Contract.Misc.Settings.ServerSetting.MaxAttachedFileSizeBytes</c>: both
/// services load the same .env.debug/.env.prod file (see docker-compose.*.yml), so a single
/// <c>ServerSetting__MaxAttachedFileSizeBytes</c> env var keeps the website-level check and
/// this microservice's hard cap in sync instead of drifting apart.
/// </summary>
public class FileStorageSetting
{
    /// <summary>Maximum allowed upload file size in bytes. Defaults to 10 MB when not configured.</summary>
    public long MaxAttachedFileSizeBytes { get; set; } = 10L * 1024 * 1024;
}
