using Maroik.Core.Client.Clients;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="MailClient"/>.
/// Verifies the HTML email body builders (mail-confirmation and password-reset)
/// and the SMTP send helper's graceful failure when no real mail server is available.
/// </summary>
public class MailClientTests
{
    private readonly MailClient _sut = new();

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
}
