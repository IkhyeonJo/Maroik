namespace Maroik.Core.Contract.Utilities;

public class ServerSetting
{
    public string? DomainName { get; set; }
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; }
    public bool SmtpSsl { get; set; }
    public string? FromEmail { get; set; }
    public string? FromFullName { get; set; }
    public bool IsDefault { get; set; }
    public byte MaxLoginAttempt { get; set; }
    public int SessionExpireMinutes { get; set; }
    public int NoticeMaturityDateDay { get; set; }
    public string? DockerCertPath { get; set; }
    public string? DockerKeyPath { get; set; }
    public string? RsaPrivateKey { get; set; }
    public string? RsaPublicKey { get; set; }
    public RsaType RsaAlgorithm { get; set; }
    public string? ClamavHost { get; set; }
    public int ClamavPort { get; set; }
    public string? FileStorageBaseUrl { get; set; }
}
