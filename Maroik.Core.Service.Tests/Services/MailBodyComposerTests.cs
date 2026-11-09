using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="MailBodyComposer"/>: the HTML bodies of the confirmation, password-reset and reminder mails.</summary>
public class MailBodyComposerTests
{
    /// <summary>The composer under test.</summary>
    private readonly MailBodyComposer _sut = new();

    // -- GetCalendarReminderBody ----------------------------------------------

    /// <summary>
    /// The reminder mail shows the event's title, time, place and calendar under Korean and English labels, and links to the
    /// owner's calendar page; every value is HTML-encoded (the title and place are user text).
    /// </summary>
    [Fact]
    public void GetCalendarReminderBody_ShowsTheEvent_AndLinksToTheCalendar_EncodingEveryValue()
    {
        string body = _sut.GetCalendarReminderBody(
            new CalendarReminderMail("<b>Dentist</b>", "2026-11-02 18:00 – 19:00 (Asia/Seoul)", "Room \"A\"", "Home & Work", "Calendar/UserIndex"),
            "https://www.maroik.com/");

        Assert.Contains("&lt;b&gt;Dentist&lt;/b&gt;", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>Dentist", body, StringComparison.Ordinal);
        Assert.Contains("일시 / When: 2026-11-02 18:00 – 19:00 (Asia/Seoul)", body, StringComparison.Ordinal);
        Assert.Contains("장소 / Where: Room &quot;A&quot;", body, StringComparison.Ordinal);
        Assert.Contains("달력 / Calendar: Home &amp; Work", body, StringComparison.Ordinal);
        Assert.Contains("href='https://www.maroik.com/Calendar/UserIndex'", body, StringComparison.Ordinal);
        Assert.Contains("일정 알림 / Event reminder", body, StringComparison.Ordinal);
    }

    /// <summary>An event without a place leaves the place line out.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void GetCalendarReminderBody_LeavesOutAMissingPlace(string? location)
    {
        string body = _sut.GetCalendarReminderBody(
            new CalendarReminderMail("T", "2026-11-02", location, "Home", "Calendar/AdminIndex"), "http://192.168.0.10:8080");

        Assert.DoesNotContain("Where", body, StringComparison.Ordinal);
        Assert.Contains("href='http://192.168.0.10:8080/Calendar/AdminIndex'", body, StringComparison.Ordinal);
    }

    // -- GetMailConfirmationBody ----------------------------------------------

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> contains title and contents.</summary>
    [Fact]
    public void GetMailConfirmationBody_ContainsTitleAndContents()
    {
        string body = _sut.GetMailConfirmationBody(
            "token123", "MyTitle", "Line0", "Line1", "https://example.com/");

        Assert.Contains("MyTitle", body, StringComparison.Ordinal);
        Assert.Contains("Line0", body, StringComparison.Ordinal);
        Assert.Contains("Line1", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> contains confirm email url.</summary>
    [Fact]
    public void GetMailConfirmationBody_ContainsConfirmEmailUrl()
    {
        string body = _sut.GetMailConfirmationBody(
            "token123", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("Account/ConfirmEmail", body, StringComparison.Ordinal);
        Assert.Contains("token123", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> url encodes special characters in token.</summary>
    [Fact]
    public void GetMailConfirmationBody_UrlEncodesSpecialCharactersInToken()
    {
        // '+' must become '%2B', '/' must become '%2F', '=' must become '%3D'
        string body = _sut.GetMailConfirmationBody(
            "a+b/c=", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("%2B", body, StringComparison.Ordinal);   // '+' encoded
        Assert.DoesNotContain("registrationToken=a+b", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> returns HTML div.</summary>
    [Fact]
    public void GetMailConfirmationBody_ReturnsHtmlDiv()
    {
        string body = _sut.GetMailConfirmationBody(
            "tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("<div", body, StringComparison.Ordinal);
        Assert.Contains("</div>", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailConfirmationBody</c> url contains domain name.</summary>
    [Fact]
    public void GetMailConfirmationBody_UrlContainsDomainName()
    {
        string body = _sut.GetMailConfirmationBody(
            "tok", "T", "C0", "C1", "https://mysite.io/");

        Assert.Contains("https://mysite.io/Account/ConfirmEmail", body, StringComparison.Ordinal);
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

        Assert.Contains("https://mysite.io/Account/ConfirmEmail", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ioAccount/ConfirmEmail", body, StringComparison.Ordinal);
    }

    // -- GetMailResetPasswordBody ---------------------------------------------

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> contains title and contents.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ContainsTitleAndContents()
    {
        string body = _sut.GetMailResetPasswordBody(
            "reset-tok", "ResetTitle", "Step0", "Step1", "https://example.com/");

        Assert.Contains("ResetTitle", body, StringComparison.Ordinal);
        Assert.Contains("Step0", body, StringComparison.Ordinal);
        Assert.Contains("Step1", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> contains reset password url.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ContainsResetPasswordUrl()
    {
        string body = _sut.GetMailResetPasswordBody(
            "reset-tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("Account/ResetPassword", body, StringComparison.Ordinal);
        Assert.Contains("reset-tok", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> url encodes special characters.</summary>
    [Fact]
    public void GetMailResetPasswordBody_UrlEncodesSpecialCharacters()
    {
        string body = _sut.GetMailResetPasswordBody(
            "x+y=z", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("%2B", body, StringComparison.Ordinal);
        Assert.Contains("%3D", body, StringComparison.Ordinal);
    }

    /// <summary>Verifies that <c>GetMailResetPasswordBody</c> returns HTML div.</summary>
    [Fact]
    public void GetMailResetPasswordBody_ReturnsHtmlDiv()
    {
        string body = _sut.GetMailResetPasswordBody(
            "tok", "T", "C0", "C1", "https://example.com/");

        Assert.Contains("<div", body, StringComparison.Ordinal);
        Assert.Contains("</div>", body, StringComparison.Ordinal);
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

        Assert.Contains("https://mysite.io/Account/ResetPassword", body, StringComparison.Ordinal);
        Assert.DoesNotContain("ioAccount/ResetPassword", body, StringComparison.Ordinal);
    }

    // -- HTML-encoding of everything interpolated into the mail body -----------------------

    /// <summary>The title and the two content lines are HTML-encoded: text in them can never become markup.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MailBodies_HtmlEncodeTheTitleAndContentLines(bool confirmation)
    {
        string body = confirmation
            ? _sut.GetMailConfirmationBody("tok", "<script>alert(1)</script>", "a <b>bold</b> line", "x & y", "https://example.com/")
            : _sut.GetMailResetPasswordBody("tok", "<script>alert(1)</script>", "a <b>bold</b> line", "x & y", "https://example.com/");

        Assert.DoesNotContain("<script>", body, StringComparison.Ordinal);
        Assert.DoesNotContain("<b>", body, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", body, StringComparison.Ordinal);
        Assert.Contains("a &lt;b&gt;bold&lt;/b&gt; line", body, StringComparison.Ordinal);
        Assert.Contains("x &amp; y", body, StringComparison.Ordinal);
    }

    /// <summary>A domain name that contains a quote cannot break out of the link's href attribute.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MailBodies_HtmlEncodeTheLink(bool confirmation)
    {
        const string hostile = "https://example.com/' onmouseover='alert(1)";
        string body = confirmation
            ? _sut.GetMailConfirmationBody("tok", "T", "C0", "C1", hostile)
            : _sut.GetMailResetPasswordBody("tok", "T", "C0", "C1", hostile);

        Assert.DoesNotContain("' onmouseover='", body, StringComparison.Ordinal);
        Assert.Contains("&#39; onmouseover=&#39;", body, StringComparison.Ordinal);
    }
}
