namespace Maroik.Common.Miscellaneous.Utilities;

public class Mail
{
    public string SmtpUserName { get; } = ServerSetting.SmtpUserName ?? string.Empty;
    public string SmtpPassword { get; } = ServerSetting.SmtpPassword ?? string.Empty;
    public string SmtpHost { get; } = ServerSetting.SmtpHost ?? string.Empty;
    public int SmtpPort { get; } = ServerSetting.SmtpPort;
    public bool SmtpSsl { get; } = ServerSetting.SmtpSsl;
    public string FromEmail { get; } = ServerSetting.FromEmail ?? string.Empty;
    public string FromFullName { get; } = ServerSetting.FromFullName ?? string.Empty;
    public List<string> ToMailIds { get; set; } = [];
    public string Subject { get; init; } = "";
    public string Body { get; set; } = "";
    public static bool IsBodyHtml => true;
    public List<string> Attachments { get; set; } = [];
}