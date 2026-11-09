using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Client interface for sending a composed email via SMTP (the account is configured in the host's settings).
/// </summary>
public interface IMailClient
{
    /// <summary>
    /// Sends an HTML email via SMTP. Returns a successful <see cref="ServiceResult"/>, or a
    /// <see cref="ServiceErrorType.Failure"/> carrying a generic message (the exception itself is logged,
    /// never returned, so SMTP server details cannot reach a caller).
    /// </summary>
    public Task<ServiceResult> SendMailAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
