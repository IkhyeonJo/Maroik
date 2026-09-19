using System.Text.RegularExpressions;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Account;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the business-logic-bearing POST actions of <c>AccountController</c> —
/// <c>Register</c>, <c>ConfirmEmail</c>, <c>ForgotPassword</c>, <c>ResetPassword</c> — that
/// <c>AccountControllerTests</c> deliberately left uncovered beyond their GET/invalid-token
/// smoke tests. These four flows carry real controller-level branching (consent/timezone
/// checks, the shared <c>LoginInputViewModel</c>'s per-action <c>ModelState.Remove</c> calls,
/// register-vs-resend routing) that this session has repeatedly found bugs in elsewhere
/// (<c>ManagementController</c>'s TimeZoneIanaId/Password dead-code fixes), so they were worth
/// closing rather than leaving as smoke-only.
///
/// Two things had to be fixed in <see cref="MaroikWebApplicationFactory"/> before any of this
/// was even testable: the RSA key settings were empty strings (every one of these flows
/// encrypts/decrypts a token, so all four would throw "RSA private/public key is not
/// configured"), and there was no substitute for the RabbitMQ-backed <c>IEmailPublisher</c>
/// (Register/ForgotPassword both publish a confirmation/reset email). Both are now real: a
/// freshly generated RSA keypair, and <see cref="FakeEmailPublisher"/> recording what would have
/// been sent.
/// </summary>
[Collection("Website Integration")]
public class AccountControllerRegistrationTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of AccountControllerRegistrationTests, sharing the MaroikWebApplicationFactory fixture across the tests in this class.</summary>
    public AccountControllerRegistrationTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private static string UniqueEmail([System.Runtime.CompilerServices.CallerMemberName] string testName = "") =>
        $"{testName.ToLowerInvariant()}-{Guid.NewGuid():N}@test.com";

    /// <summary>Fetches a page's antiforgery cookie + hidden-input token pair for an anonymous POST.</summary>
    private async Task<(string Cookie, string Token)> GetAntiForgeryAsync(string getUrl)
    {
        var response = await _client.GetAsync(getUrl, TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        string token = ExtractAntiForgeryToken(html);
        string? cookie = ExtractCookieValue(response, AntiForgeryCookieName);
        Assert.False(string.IsNullOrEmpty(cookie));
        return (cookie!, token);
    }

    private async Task<HttpResponseMessage> PostFormAsync(string url, Dictionary<string, string> fields)
    {
        var (cookie, token) = await GetAntiForgeryAsync(url);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", $"{AntiForgeryCookieName}={cookie}");
        fields["__RequestVerificationToken"] = token;
        request.Content = new FormUrlEncodedContent(fields);
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private const string AntiForgeryCookieName = "__Secure-.AspNetCore.Antiforgery.";

    private static string? ExtractCookieValue(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders)) return null;
        foreach (string header in setCookieHeaders)
        {
            string namePart = header.Split(';', 2)[0];
            int eq = namePart.IndexOf('=');
            if (eq <= 0) continue;
            if (namePart[..eq] == cookieName) return namePart[(eq + 1)..];
        }
        return null;
    }

    private static string ExtractAntiForgeryToken(string html)
    {
        Match match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Could not find __RequestVerificationToken in the page HTML.");
        return match.Groups[1].Value;
    }

    private async Task SeedAccountAsync(string email, bool emailConfirmed, string? registrationToken = null, string? resetPasswordToken = null, string? nickname = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        db.Accounts.Add(new Maroik.Core.PostgreSQL.Models.Account
        {
            Email = email,
            HashedPassword = passwordService.HashPassword("OldPassword1!"),
            Nickname = nickname ?? "RegTest_" + email.Replace("@", "_").Replace(".", "_"),
            AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
            Role = Role.User,
            TimeZoneIanaId = "UTC",
            Locked = false,
            LoginAttempt = 0,
            EmailConfirmed = emailConfirmed,
            AgreedServiceTerms = true,
            RegistrationToken = registrationToken,
            ResetPasswordToken = resetPasswordToken,
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // -- Register: success path -----------------------------------------------------

    /// <summary>Register new account creates unconfirmed account and publishes email.</summary>
    [Fact]
    public async Task Register_NewAccount_CreatesUnconfirmedAccountAndPublishesEmail()
    {
        string email = UniqueEmail();
        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "NewPassword1!",
            ["Nickname"] = "RegSuccess_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var created = db.Accounts.Single(a => a.Email == email);
        Assert.False(created.EmailConfirmed);
        Assert.False(string.IsNullOrEmpty(created.RegistrationToken));

        var publisher = scope.ServiceProvider.GetRequiredService<FakeEmailPublisher>();
        Assert.Contains(publisher.PublishedMessages, m => m.ToEmail == email);
    }

    /// <summary>Register missing consent does not create account.</summary>
    [Fact]
    public async Task Register_MissingConsent_DoesNotCreateAccount()
    {
        string email = UniqueEmail();
        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "NewPassword1!",
            ["Nickname"] = "RegNoConsent_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "false",
            ["TimeZoneIanaId"] = "UTC"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Accounts.Any(a => a.Email == email));
    }

    /// <summary>Register missing time zone does not create account.</summary>
    [Fact]
    public async Task Register_MissingTimeZone_DoesNotCreateAccount()
    {
        string email = UniqueEmail();
        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "NewPassword1!",
            ["Nickname"] = "RegNoTz_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = ""
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(db.Accounts.Any(a => a.Email == email));
    }

    /// <summary>Register already confirmed email does not overwrite account.</summary>
    [Fact]
    public async Task Register_AlreadyConfirmedEmail_DoesNotOverwriteAccount()
    {
        string email = UniqueEmail();
        await SeedAccountAsync(email, emailConfirmed: true);

        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "AnotherPassword1!",
            ["Nickname"] = "RegDup_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.True(account.EmailConfirmed); // unchanged
    }

    /// <summary>
    /// The "nickname already exists" message is stored raw in <c>TempData</c> and HTML-encoded once by the
    /// view. Regression: it used to be encoded twice (once when built, once by the view), so a Korean
    /// nickname was shown as <c>&amp;#xD64D;…</c> and <c>R&amp;D</c> as <c>R&amp;amp;D</c>.
    /// </summary>
    [Theory]
    [InlineData("홍길동")]
    [InlineData("R&D팀")]
    [InlineData("O'Brien")]
    [InlineData("PlainAscii")]
    public async Task Register_TakenNickname_ShowsTheNicknameEncodedOnlyOnce(string nickname)
    {
        await SeedAccountAsync(UniqueEmail(), emailConfirmed: true, nickname: nickname);

        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = UniqueEmail(),
            ["Password"] = "AnotherPassword1!",
            ["ConfirmPassword"] = "AnotherPassword1!",
            ["Nickname"] = nickname,
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Match error = Regex.Match(html, "<span style=\"font-weight:bold; color: red; \">(.*?)</span>", RegexOptions.Singleline);
        Assert.True(error.Success, "The registration error message was not rendered.");

        // What the browser shows is the HTML-decoded text; it must read exactly like the typed nickname.
        Assert.StartsWith($"'{nickname}' ", System.Net.WebUtility.HtmlDecode(error.Groups[1].Value));
    }

    /// <summary>
    /// The "not a valid email address" message carries the typed address as a format argument. It must
    /// read like the typed text (once-encoded), not with entities: <c>+</c>, <c>'</c> and <c>@</c> are all
    /// characters the default HTML encoder escapes. "user@localhost" passes the view model's
    /// <c>[EmailAddress]</c> attribute but is rejected by the domain's <c>Email</c> value object, which is
    /// what surfaces the message.
    /// </summary>
    [Fact]
    public async Task Register_InvalidEmailForDomain_ShowsTheAddressEncodedOnlyOnce()
    {
        const string typed = "o'brien+x@localhost";

        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = typed,
            ["Password"] = "AnotherPassword1!",
            ["ConfirmPassword"] = "AnotherPassword1!",
            ["Nickname"] = "EmailMsg_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Match error = Regex.Match(html, "<span style=\"font-weight:bold; color: red; \">(.*?)</span>", RegexOptions.Singleline);
        Assert.True(error.Success, "The registration error message was not rendered.");

        Assert.StartsWith($"'{typed}' ", System.Net.WebUtility.HtmlDecode(error.Groups[1].Value));
    }

    /// <summary>Register unconfirmed email resends confirmation on resubmission.</summary>
    [Fact]
    public async Task Register_UnconfirmedEmail_ResendsConfirmationOnResubmission()
    {
        string email = UniqueEmail();
        await SeedAccountAsync(email, emailConfirmed: false, registrationToken: null);

        // Resend path: submitting with an empty Password re-triggers the confirmation email
        // rather than attempting to create a second account.
        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "",
            ["Nickname"] = "",
            ["AgreedServiceTerms"] = "false",
            ["TimeZoneIanaId"] = ""
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.False(string.IsNullOrEmpty(account.RegistrationToken)); // regenerated

        var publisher = scope.ServiceProvider.GetRequiredService<FakeEmailPublisher>();
        Assert.Contains(publisher.PublishedMessages, m => m.ToEmail == email);
    }

    /// <summary>
    /// The "you keep failing to receive the mail" state links the administrator with a well-formed
    /// <c>mailto:</c> URL (it once had a stray space after the colon).
    /// </summary>
    [Fact]
    public async Task Register_UnconfirmedEmail_Resubmission_ShowsAWellFormedAdministratorMailtoLink()
    {
        string email = UniqueEmail();
        await SeedAccountAsync(email, emailConfirmed: false, registrationToken: null);

        var response = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "",
            ["Nickname"] = "",
            ["AgreedServiceTerms"] = "false",
            ["TimeZoneIanaId"] = ""
        });
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("mailto:admin@maroik.com", html);
        Assert.DoesNotContain("mailto: ", html);
    }

    // -- ConfirmEmail -----------------------------------------------------------------

    /// <summary>Confirm email valid token activates account.</summary>
    [Fact]
    public async Task ConfirmEmail_ValidToken_ActivatesAccount()
    {
        string email = UniqueEmail();
        string rawToken = GuidToken.Generate();
        await SeedAccountAsync(email, emailConfirmed: false, registrationToken: rawToken);

        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(rawToken);

        var response = await _client.GetAsync($"/Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(encryptedToken)}", TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.True(account.EmailConfirmed);
    }

    /// <summary>Confirm email already confirmed does not error.</summary>
    [Fact]
    public async Task ConfirmEmail_AlreadyConfirmed_DoesNotError()
    {
        string email = UniqueEmail();
        string rawToken = GuidToken.Generate();
        await SeedAccountAsync(email, emailConfirmed: true, registrationToken: rawToken);

        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(rawToken);

        var response = await _client.GetAsync($"/Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(encryptedToken)}", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- ForgotPassword -----------------------------------------------------------------

    /// <summary>Forgot password confirmed account sets reset token and publishes email.</summary>
    [Fact]
    public async Task ForgotPassword_ConfirmedAccount_SetsResetTokenAndPublishesEmail()
    {
        string email = UniqueEmail();
        await SeedAccountAsync(email, emailConfirmed: true);

        var response = await PostFormAsync("/Account/ForgotPassword", new Dictionary<string, string> { ["Email"] = email });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.False(string.IsNullOrEmpty(account.ResetPasswordToken));

        var publisher = scope.ServiceProvider.GetRequiredService<FakeEmailPublisher>();
        Assert.Contains(publisher.PublishedMessages, m => m.ToEmail == email);
    }

    /// <summary>Forgot password unknown email silently succeeds without publishing.</summary>
    [Fact]
    public async Task ForgotPassword_UnknownEmail_SilentlySucceedsWithoutPublishing()
    {
        string email = UniqueEmail();

        var response = await PostFormAsync("/Account/ForgotPassword", new Dictionary<string, string> { ["Email"] = email });

        // Silently succeeds (200) to avoid user enumeration, even though the account doesn't exist.
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<FakeEmailPublisher>();
        Assert.DoesNotContain(publisher.PublishedMessages, m => m.ToEmail == email);
    }

    // -- ResetPassword ------------------------------------------------------------------

    /// <summary>Reset password get valid token does not fail.</summary>
    [Fact]
    public async Task ResetPassword_Get_ValidToken_DoesNotFail()
    {
        string email = UniqueEmail();
        string rawToken = GuidToken.Generate();
        await SeedAccountAsync(email, emailConfirmed: true, resetPasswordToken: rawToken);

        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(rawToken);

        var response = await _client.GetAsync($"/Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(encryptedToken)}", TestContext.Current.CancellationToken);
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        // A valid token renders the new-password form (id="resetPasswordForm"); an invalid one
        // renders "The authentication token is invalid." instead — see ResetPassword.cshtml.
        Assert.Contains("resetPasswordForm", html);
    }

    /// <summary>Reset password post valid token changes password and allows login with new password.</summary>
    [Fact]
    public async Task ResetPassword_Post_ValidToken_ChangesPasswordAndAllowsLoginWithNewPassword()
    {
        string email = UniqueEmail();
        string rawToken = GuidToken.Generate();
        await SeedAccountAsync(email, emailConfirmed: true, resetPasswordToken: rawToken);

        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(rawToken);

        var response = await PostFormAsync("/Account/ResetPassword", new Dictionary<string, string>
        {
            ["ResetPasswordToken"] = encryptedToken,
            ["Password"] = "BrandNewPassword1!"
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        // Prove the new password actually works end-to-end via the real Login action.
        var (loginCookie, loginToken) = await GetAntiForgeryAsync("/Account/Login");
        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");
        loginRequest.Headers.Add("Cookie", $"{AntiForgeryCookieName}={loginCookie}");
        loginRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "BrandNewPassword1!",
            ["__RequestVerificationToken"] = loginToken
        });
        var loginResponse = await _client.SendAsync(loginRequest, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, loginResponse.StatusCode); // login success redirects; failure re-renders as 200
    }

    /// <summary>Reset password post unknown token returns fail to reset.</summary>
    [Fact]
    public async Task ResetPassword_Post_UnknownToken_ReturnsFailToReset()
    {
        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(GuidToken.Generate()); // well-formed but not stored on any account

        var response = await PostFormAsync("/Account/ResetPassword", new Dictionary<string, string>
        {
            ["ResetPasswordToken"] = encryptedToken,
            ["Password"] = "WontBeApplied1!"
        });
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The authentication token is invalid.", html);
    }

    /// <summary>
    /// Regression: a new password over 72 UTF-8 bytes (here 3 + 25 Hangul = 78 bytes, but well under 100
    /// characters) used to be reported as "The authentication token is invalid." — although nothing was wrong
    /// with the token. The form must come back (with the token) and say what is wrong with the password, and
    /// the reset token must stay usable.
    /// </summary>
    [Fact]
    public async Task ResetPassword_Post_PasswordOver72Bytes_ReShowsTheFormAndKeepsTheToken()
    {
        string email = UniqueEmail();
        string rawToken = GuidToken.Generate();
        await SeedAccountAsync(email, emailConfirmed: true, resetPasswordToken: rawToken);

        using var scope = _factory.Services.CreateScope();
        var rsa = scope.ServiceProvider.GetRequiredService<IRsaService>();
        string encryptedToken = rsa.Encrypt(rawToken);

        var response = await PostFormAsync("/Account/ResetPassword", new Dictionary<string, string>
        {
            ["ResetPasswordToken"] = encryptedToken,
            ["Password"] = "Aa1" + new string('가', 25)
        });
        string html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("resetPasswordForm", html);
        Assert.DoesNotContain("The authentication token is invalid.", html);
        Assert.Contains("at most 72 bytes", html);

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.Equal(rawToken, account.ResetPasswordToken); // still usable
    }

    /// <summary>
    /// Regression: a registration the service rejects (here a reserved nickname) sent no mail, so it
    /// must not burn the per-address e-mail cooldown — the corrected form, submitted straight away,
    /// has to go through instead of being told to "wait a moment before requesting another email".
    /// </summary>
    [Fact]
    public async Task Register_RejectedByService_DoesNotConsumeTheEmailCooldown()
    {
        string email = UniqueEmail();

        var rejected = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "NewPassword1!",
            ["Nickname"] = "Admin",
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, rejected.StatusCode);

        var accepted = await PostFormAsync("/Account/Register", new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "NewPassword1!",
            ["Nickname"] = "RegRetry_" + Guid.NewGuid().ToString("N")[..8],
            ["AgreedServiceTerms"] = "true",
            ["TimeZoneIanaId"] = "UTC"
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, accepted.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(db.Accounts.Any(a => a.Email == email), "the corrected registration was throttled");
        var publisher = scope.ServiceProvider.GetRequiredService<FakeEmailPublisher>();
        Assert.Contains(publisher.PublishedMessages, m => m.ToEmail == email);
    }
}
