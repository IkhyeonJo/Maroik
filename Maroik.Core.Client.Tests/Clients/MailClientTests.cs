using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using MailKit.Security;
using Maroik.Core.Client.Clients;
using Maroik.Core.Client.Tests.Fixtures;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="MailClient"/>.
/// Verifies the certificate rules and the SMTP send, also when no real mail server is available.
/// </summary>
public class MailClientTests
{
    // -- ShouldAcceptCertificate ------------------------------------------
    // This is the sole gate distinguishing "tolerate a self-signed relay" from "accept a MITM"
    // when SmtpSsl is disabled. Each SslPolicyErrors branch is asserted explicitly so a future edit
    // that merges cases or flips a default can never silently reopen the MITM hole undetected.

    /// <summary>No certificate error at all is always accepted.</summary>
    [Fact]
    public void ShouldAcceptCertificate_Accepts_WhenNoError()
    {
        Assert.True(MailClient.ShouldAcceptCertificate(SslPolicyErrors.None));
    }

    /// <summary>A bare chain / self-signed-root problem is accepted (the "operator's own relay" case).</summary>
    [Fact]
    public void ShouldAcceptCertificate_Accepts_WhenOnlyChainErrors()
    {
        Assert.True(MailClient.ShouldAcceptCertificate(SslPolicyErrors.RemoteCertificateChainErrors));
    }

    /// <summary>A hostname mismatch is rejected outright — the signature of an active MITM.</summary>
    [Fact]
    public void ShouldAcceptCertificate_Rejects_WhenNameMismatch()
    {
        Assert.False(MailClient.ShouldAcceptCertificate(SslPolicyErrors.RemoteCertificateNameMismatch));
    }

    /// <summary>A missing certificate is rejected outright.</summary>
    [Fact]
    public void ShouldAcceptCertificate_Rejects_WhenCertificateNotAvailable()
    {
        Assert.False(MailClient.ShouldAcceptCertificate(SslPolicyErrors.RemoteCertificateNotAvailable));
    }

    /// <summary>Combined error flags (e.g. chain errors plus a name mismatch) must reject, not accept.</summary>
    [Fact]
    public void ShouldAcceptCertificate_Rejects_WhenChainErrorsCombinedWithNameMismatch()
    {
        const SslPolicyErrors combined = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch;

        Assert.False(MailClient.ShouldAcceptCertificate(combined));
    }

    // -- GetSecureSocketOptions -------------------------------------------
    // SmtpSsl on must REQUIRE TLS. The old mapping (SecureSocketOptions.Auto) continued in cleartext
    // when the server did not advertise STARTTLS, so an attacker stripping that capability got the
    // SMTP credentials and the token-bearing mail body unencrypted.

    /// <summary>SmtpSsl on with a submission port requires STARTTLS — never the cleartext-tolerant Auto / StartTlsWhenAvailable.</summary>
    [Theory]
    [InlineData(587)]
    [InlineData(25)]
    [InlineData(2525)]
    public void GetSecureSocketOptions_SslOn_RequiresStartTls(int port)
    {
        Assert.Equal(SecureSocketOptions.StartTls, MailClient.GetSecureSocketOptions(true, port));
    }

    /// <summary>SmtpSsl on with a conventional implicit-TLS port uses TLS from the first byte.</summary>
    [Theory]
    [InlineData(465)]
    [InlineData(2465)]
    public void GetSecureSocketOptions_SslOn_ImplicitTlsPorts_UseSslOnConnect(int port)
    {
        Assert.Equal(SecureSocketOptions.SslOnConnect, MailClient.GetSecureSocketOptions(true, port));
    }

    /// <summary>SmtpSsl on never maps to an option that tolerates a server without TLS.</summary>
    [Theory]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2465)]
    [InlineData(0)]
    public void GetSecureSocketOptions_SslOn_NeverAllowsCleartext(int port)
    {
        SecureSocketOptions options = MailClient.GetSecureSocketOptions(true, port);

        Assert.NotEqual(SecureSocketOptions.Auto, options);
        Assert.NotEqual(SecureSocketOptions.StartTlsWhenAvailable, options);
        Assert.NotEqual(SecureSocketOptions.None, options);
    }

    /// <summary>SmtpSsl off keeps the deliberate opportunistic STARTTLS for an operator's own relay, on any port.</summary>
    [Theory]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    public void GetSecureSocketOptions_SslOff_IsOpportunisticStartTls(int port)
    {
        Assert.Equal(SecureSocketOptions.StartTlsWhenAvailable, MailClient.GetSecureSocketOptions(false, port));
    }

    // -- SendMailAsync (through the real MailKit client against an in-process SMTP server) --------

    /// <summary>SMTP settings pointing at a local fake server on <paramref name="port"/> (no TLS required).</summary>
    private static ServerSetting SettingsFor(int port, string password = "s3cret") => new()
    {
        SmtpHost = "127.0.0.1",
        SmtpPort = port,
        SmtpSsl = false,
        SmtpUserName = "mailer",
        SmtpPassword = password,
        FromEmail = "noreply@maroik.test",
        FromFullName = "Maroik"
    };

    /// <summary>A client whose settings point at the local fake server on <paramref name="port"/>.</summary>
    private static MailClient ClientFor(int port, string password = "s3cret") =>
        new(NullLogger<MailClient>.Instance, Options.Create(SettingsFor(port, password)));

    /// <summary>A message is delivered with the configured sender, the recipient, the subject and the HTML body, after authenticating.</summary>
    [Fact]
    public async Task SendMailAsync_DeliversTheMessage_AfterAuthenticating()
    {
        await using var smtp = new FakeSmtpServer();

        ServiceResult result = await ClientFor(smtp.Port).SendMailAsync("bob@example.com", "Hello there", "<p>confirm me</p>", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(("mailer", "s3cret"), smtp.Credentials);
        Assert.Contains(smtp.Commands, c => c.StartsWith("MAIL FROM:<noreply@maroik.test>", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(smtp.Commands, c => c.StartsWith("RCPT TO:<bob@example.com>", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(smtp.ReceivedMessage);
        Assert.Contains("Subject: Hello there", smtp.ReceivedMessage, StringComparison.Ordinal);
        Assert.Contains("bob@example.com", smtp.ReceivedMessage, StringComparison.Ordinal);
        Assert.Contains("Content-Type: text/html", smtp.ReceivedMessage, StringComparison.Ordinal);
        Assert.Contains("<p>confirm me</p>", smtp.ReceivedMessage, StringComparison.Ordinal);
    }

    /// <summary>
    /// The server has accepted the message, so a failure while closing the connection must NOT be
    /// reported as a failed send (the caller would republish it and the recipient get a duplicate).
    /// This pins that observable contract; note that MailKit itself already swallows a failed QUIT, so
    /// this does not exercise <c>MailClient</c>'s own defensive catch around <c>DisconnectAsync</c> — that one is driven by
    /// <c>SendMailAsync_AFailureWhileClosingTheConnection_StillReportsSuccess_AndOnlyLogsAWarning</c> through a substituted SMTP client.
    /// </summary>
    [Fact]
    public async Task SendMailAsync_StillReportsSuccess_WhenClosingTheConnectionFailsAfterTheServerAcceptedTheMessage()
    {
        await using var smtp = new FakeSmtpServer(dropConnectionOnQuit: true);

        ServiceResult result = await ClientFor(smtp.Port).SendMailAsync("bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(smtp.ReceivedMessage);
    }

    /// <summary>Rejected credentials fail to send with a generic message that leaks neither the host nor the server's reply, and nothing is delivered.</summary>
    [Fact]
    public async Task SendMailAsync_ReturnsAGenericFailure_WhenTheServerRejectsTheCredentials()
    {
        await using var smtp = new FakeSmtpServer(rejectAuthentication: true);

        ServiceResult result = await ClientFor(smtp.Port, "wrong").SendMailAsync("bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Mail.SendFailed", result.ErrorCode);
        Assert.DoesNotContain("127.0.0.1", result.ErrorKey, StringComparison.Ordinal);
        Assert.DoesNotContain("535", result.ErrorKey, StringComparison.Ordinal);
        Assert.Null(smtp.ReceivedMessage);
    }

    /// <summary>An unreachable server is a failed send, not an exception.</summary>
    [Fact]
    public async Task SendMailAsync_ReturnsAGenericFailure_WhenTheServerIsUnreachable()
    {
        int closedPort;
        await using (var probe = new FakeSmtpServer()) closedPort = probe.Port;

        var logger = new RecordingLogger();

        ServiceResult result = await new MailClient(logger, Options.Create(SettingsFor(closedPort))).SendMailAsync("bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Mail.SendFailed", result.ErrorCode);
        Assert.Equal("Failed to send the email.", result.ErrorKey); // generic: no server or exception text for the caller
        var error = Assert.Single(logger.Entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Error);
        Assert.Equal("Failed to send mail to bob@example.com", error.Message);
        Assert.NotNull(error.Exception);
    }

    /// <summary>
    /// With SmtpSsl off, a certificate problem is logged either way: an untrusted chain is accepted with a warning, a name
    /// mismatch refused with one naming the errors, and a clean certificate passes silently.
    /// </summary>
    [Theory]
    [InlineData(SslPolicyErrors.RemoteCertificateChainErrors, true, "Accepting an untrusted SMTP server certificate for relay.example because SmtpSsl is disabled for this relay")]
    [InlineData(SslPolicyErrors.RemoteCertificateNameMismatch, false, "Rejecting the SMTP server certificate for relay.example: RemoteCertificateNameMismatch")]
    [InlineData(SslPolicyErrors.None, true, null)]
    public void AcceptRelaxedCertificate_DecidesAndLogsTheCertificateProblem(SslPolicyErrors errors, bool accepted, string? warning)
    {
        var logger = new RecordingLogger();

        bool result = new MailClient(logger, Options.Create(new ServerSetting())).AcceptRelaxedCertificate(errors, "relay.example");

        Assert.Equal(accepted, result);
        Assert.Equal(warning is null ? [] : [warning], logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).Select(e => e.Message));
    }

    /// <summary>A malformed recipient address is a failed send, not an exception.</summary>
    [Fact]
    public async Task SendMailAsync_ReturnsAGenericFailure_WhenTheRecipientIsNotAValidAddress()
    {
        await using var smtp = new FakeSmtpServer();

        ServiceResult result = await ClientFor(smtp.Port).SendMailAsync("not an address\r\nBcc: victim@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.DoesNotContain(smtp.Commands, c => c.Contains("victim@example.com", StringComparison.Ordinal));
    }

    // -- SmtpSsl off: opportunistic STARTTLS with a relaxed (chain-only) certificate check ---------------

    /// <summary>A self-signed certificate whose subject-alternative names are set by <paramref name="san"/>.</summary>
    private static X509Certificate2 SelfSigned(Action<SubjectAlternativeNameBuilder> san)
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=fake.smtp", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        san(names);
        request.CertificateExtensions.Add(names.Build());
        using X509Certificate2 cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null);
    }

    /// <summary>
    /// The operator's own relay with a self-signed certificate (only an untrusted chain, the name matches) is accepted
    /// when SmtpSsl is off: the connection is upgraded with STARTTLS and the mail is delivered over it.
    /// </summary>
    [Fact]
    public async Task SendMailAsync_AcceptsASelfSignedRelayCertificate_WhenSmtpSslIsOff_AndUpgradesToTls()
    {
        using X509Certificate2 cert = SelfSigned(n => n.AddIpAddress(IPAddress.Loopback));
        await using var smtp = new FakeSmtpServer(certificate: cert);

        ServiceResult result = await ClientFor(smtp.Port).SendMailAsync("bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(smtp.TlsEstablished);
        Assert.NotNull(smtp.ReceivedMessage);
    }

    /// <summary>
    /// A certificate whose name does not match the host is the signature of a man-in-the-middle, not of a benign
    /// self-signed relay: it is rejected even though SmtpSsl is off, and nothing (credentials included) is sent.
    /// </summary>
    [Fact]
    public async Task SendMailAsync_RejectsACertificateForAnotherHost_EvenWhenSmtpSslIsOff()
    {
        using X509Certificate2 cert = SelfSigned(n => n.AddDnsName("mail.other-host.example"));
        await using var smtp = new FakeSmtpServer(certificate: cert);

        ServiceResult result = await ClientFor(smtp.Port).SendMailAsync("bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Equal("Mail.SendFailed", result.ErrorCode);
        Assert.Null(smtp.Credentials);
        Assert.Null(smtp.ReceivedMessage);
    }

    // -- closing the connection after the server accepted the mail ------------------------------------------------

    /// <summary>An SMTP client whose closing step fails, to exercise the "sent, but close failed" path.</summary>
    private sealed class FailingCloseSmtpClient : MailKit.Net.Smtp.SmtpClient
    {
        /// <summary>Always throws, as if the connection were reset while closing.</summary>
        public override Task DisconnectAsync(bool quit, CancellationToken cancellationToken = default) =>
            throw new IOException("connection reset while closing");
    }

    /// <summary>Logger that keeps every entry for assertions.</summary>
    private sealed class RecordingLogger : Microsoft.Extensions.Logging.ILogger<MailClient>
    {
        /// <summary>Every logged entry: its level, formatted message and exception.</summary>
        public List<(Microsoft.Extensions.Logging.LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];
        /// <summary>Scopes are not recorded.</summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        /// <summary>Every level is enabled.</summary>
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        /// <summary>Records the level, formatted message and exception.</summary>
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception), exception));
    }

    /// <summary>
    /// Once the server has accepted the message, a failure while closing the connection must not be reported as a failed
    /// send — the caller would republish and the recipient would get a duplicate. The send is a success; the close failure is
    /// only logged (as a warning, with the exception).
    /// </summary>
    [Fact]
    public async Task SendMailAsync_AFailureWhileClosingTheConnection_StillReportsSuccess_AndOnlyLogsAWarning()
    {
        await using var smtp = new FakeSmtpServer();
        var logger = new RecordingLogger();
        var sut = new MailClient(logger, Options.Create(SettingsFor(smtp.Port))) { SmtpClientFactory = () => new FailingCloseSmtpClient() };

        ServiceResult result = await sut.SendMailAsync(
            "bob@example.com", "Hello", "<p>hi</p>", TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(smtp.ReceivedMessage); // the mail really was accepted
        var warning = Assert.Single(logger.Entries, e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
        Assert.Contains("bob@example.com", warning.Message, StringComparison.Ordinal);
        Assert.IsType<IOException>(warning.Exception);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= Microsoft.Extensions.Logging.LogLevel.Error);
    }
}
