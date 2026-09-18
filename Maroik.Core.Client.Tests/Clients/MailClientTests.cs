using System.Net.Security;
using MailKit.Security;
using Maroik.Core.Client.Clients;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="MailClient"/>.
/// Verifies the HTML email body builders (mail-confirmation and password-reset)
/// and the SMTP send helper's graceful failure when no real mail server is available.
/// </summary>
public class MailClientTests
{
    private readonly MailClient _sut = new(NullLogger<MailClient>.Instance);

    // -- GetMailConfirmationBody ----------------------------------------------

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> contains title and contents.</summary>
    [Fact]
    public void GetMailConfirmationBody_ContainsTitleAndContents()
    {
        string body = _sut.GetMailConfirmationBody(
            "token123", "MyTitle", "Line0", "Line1", "https://example.com/");

        Assert.Contains("MyTitle", body);
        Assert.Contains("Line0", body);
        Assert.Contains("Line1", body);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> contains confirm email url.</summary>
    [Fact]
    public void GetMailConfirmationBody_ContainsConfirmEmailUrl()
    {
        string body = _sut.GetMailConfirmationBody(
            "token123", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("Account/ConfirmEmail", body);
        Assert.Contains("token123", body);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> url encodes special characters in token.</summary>
    [Fact]
    public void GetMailConfirmationBody_UrlEncodesSpecialCharactersInToken()
    {
        // '+' must become '%2B', '/' must become '%2F', '=' must become '%3D'
        string body = _sut.GetMailConfirmationBody(
            "a+b/c=", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("%2B", body);   // '+' encoded
        Assert.DoesNotContain("registrationToken=a+b", body);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> returns html div.</summary>
    [Fact]
    public void GetMailConfirmationBody_ReturnsHtmlDiv()
    {
        string body = _sut.GetMailConfirmationBody(
            "tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("<div", body);
        Assert.Contains("</div>", body);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> url contains domain name.</summary>
    [Fact]
    public void GetMailConfirmationBody_UrlContainsDomainName()
    {
        string body = _sut.GetMailConfirmationBody(
            "tok", "T", "C0", "C1", "https://mysite.io/");

        Assert.Contains("https://mysite.io/Account/ConfirmEmail", body);
    }

    /// <summary>
    /// Verifies that a <c>DomainName</c> configured without a trailing slash still yields a
    /// well-formed link (regression for "https://mysite.ioAccount/ConfirmEmail").
    /// </summary>
    [Theory]
    [InlineData("https://mysite.io")]
    [InlineData("https://mysite.io/")]
    public void GetMailConfirmationBody_JoinsDomainNameRegardlessOfTrailingSlash(string domainName)
    {
        string body = _sut.GetMailConfirmationBody("tok", "T", "C0", "C1", domainName);

        Assert.Contains("https://mysite.io/Account/ConfirmEmail", body);
        Assert.DoesNotContain("ioAccount/ConfirmEmail", body);
    }

    // -- GetMailResetPasswordBody ---------------------------------------------

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> contains title and contents.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ContainsTitleAndContents()
    {
        string body = _sut.GetMailResetPasswordBody(
            "reset-tok", "ResetTitle", "Step0", "Step1", "https://example.com/");

        Assert.Contains("ResetTitle", body);
        Assert.Contains("Step0", body);
        Assert.Contains("Step1", body);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> contains reset password url.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ContainsResetPasswordUrl()
    {
        string body = _sut.GetMailResetPasswordBody(
            "reset-tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("Account/ResetPassword", body);
        Assert.Contains("reset-tok", body);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> url encodes special characters.</summary>
    [Fact]
    public void GetMailResetPasswordBody_UrlEncodesSpecialCharacters()
    {
        string body = _sut.GetMailResetPasswordBody(
            "x+y=z", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("%2B", body);
        Assert.Contains("%3D", body);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> returns html div.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ReturnsHtmlDiv()
    {
        string body = _sut.GetMailResetPasswordBody(
            "tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("<div", body);
        Assert.Contains("</div>", body);
    }

    /// <summary>
    /// Verifies that a <c>DomainName</c> configured without a trailing slash still yields a
    /// well-formed reset link.
    /// </summary>
    [Theory]
    [InlineData("https://mysite.io")]
    [InlineData("https://mysite.io/")]
    public void GetMailResetPasswordBody_JoinsDomainNameRegardlessOfTrailingSlash(string domainName)
    {
        string body = _sut.GetMailResetPasswordBody("tok", "T", "C0", "C1", domainName);

        Assert.Contains("https://mysite.io/Account/ResetPassword", body);
        Assert.DoesNotContain("ioAccount/ResetPassword", body);
    }

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
        SslPolicyErrors combined = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch;

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
}
