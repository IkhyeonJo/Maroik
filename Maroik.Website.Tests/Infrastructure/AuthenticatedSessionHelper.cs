using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Maroik.Core.Contract.Misc.Constants;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Infrastructure;

/// <summary>
/// A logged-in browser session captured from a real <c>/Account/Login</c> round-trip:
/// the authenticated session cookie plus a matching antiforgery cookie/token pair, ready
/// to be attached to subsequent authenticated requests against role-gated JSON endpoints
/// (the pattern every <c>[RequiredHttpPostAccess]</c> action requires).
/// </summary>
public sealed class AuthenticatedSession(string sessionCookieValue, string antiforgeryCookieValue, string antiforgeryToken, string nickname)
{
    /// <summary>Cookie header value combining the session and antiforgery cookies.</summary>
    public string CookieHeader =>
        $"{AuthenticatedSessionHelper.SessionCookieName}={sessionCookieValue}; {AuthenticatedSessionHelper.AntiforgeryCookieName}={antiforgeryCookieValue}";

    /// <summary>The raw antiforgery token to send via the <c>RequestVerificationToken</c> header on AJAX/JSON POSTs.</summary>
    public string AntiforgeryToken => antiforgeryToken;

    /// <summary>The seeded account's nickname (derived from its email — see <see cref="AuthenticatedSessionHelper.LoginAsync"/>).</summary>
    public string Nickname => nickname;

    /// <summary>
    /// Builds a POST request against a JSON <c>[FromBody]</c> endpoint, with the session
    /// cookie and antiforgery header already attached.
    /// </summary>
    public HttpRequestMessage BuildJsonPostRequest(string url, object? body = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", CookieHeader);
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        if (body != null) request.Content = JsonContent.Create(body);
        return request;
    }

    /// <summary>
    /// Builds a POST request against a <c>[FromForm]</c>/model-bound-without-attribute endpoint
    /// (i.e. one accepting an <see cref="IFormFile"/>), with the session cookie and antiforgery
    /// header already attached.
    /// </summary>
    public HttpRequestMessage BuildFormPostRequest(string url, MultipartFormDataContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", CookieHeader);
        request.Headers.Add("RequestVerificationToken", antiforgeryToken);
        request.Content = content;
        return request;
    }
}

/// <summary>Seeds an account and drives the real <c>/Account/Login</c> flow to produce an <see cref="AuthenticatedSession"/>.</summary>
public static class AuthenticatedSessionHelper
{
    internal const string SessionCookieName = "__Secure-.AspNetCore.Session";
    internal const string AntiforgeryCookieName = "__Secure-.AspNetCore.Antiforgery.";

    /// <summary>
    /// Inserts an account directly into the database (bypassing registration/email-confirmation)
    /// and logs in as it via a real HTTP round-trip, returning the resulting session.
    /// </summary>
    public static async Task<AuthenticatedSession> LoginAsync(
        MaroikWebApplicationFactory factory, HttpClient client, string email, string password, string role, CancellationToken ct)
    {
        // Account_Nickname_unique is a real, enforced constraint (Postgres), so every seeded
        // account needs a distinct nickname — derive it from the (already-unique) email rather
        // than a fixed placeholder.
        string nickname = "TestUser_" + email.Replace("@", "_").Replace(".", "_");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
            if (!db.Accounts.Any(a => a.Email == email))
            {
                db.Accounts.Add(new Account
                {
                    Email = email,
                    HashedPassword = passwordService.HashPassword(password),
                    Nickname = nickname,
                    AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                    Role = role,
                    TimeZoneIanaId = "UTC",
                    Locked = false,
                    LoginAttempt = 0,
                    EmailConfirmed = true,
                    AgreedServiceTerms = true,
                    Deleted = false,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        }

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/Account/Login");
        var getResponse = await client.SendAsync(getRequest, ct);
        string html = await getResponse.Content.ReadAsStringAsync(ct);
        string loginToken = ExtractAntiForgeryToken(html);
        string loginAntiforgeryCookie = ExtractCookieValue(getResponse, AntiforgeryCookieName)
            ?? throw new InvalidOperationException("No antiforgery cookie on the Login GET response.");

        using var postRequest = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");
        postRequest.Headers.Add("Cookie", $"{AntiforgeryCookieName}={loginAntiforgeryCookie}");
        postRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = password,
            ["__RequestVerificationToken"] = loginToken
        });
        var postResponse = await client.SendAsync(postRequest, ct);
        if (postResponse.StatusCode != System.Net.HttpStatusCode.Redirect)
        {
            string body = await postResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Login failed for {email} ({postResponse.StatusCode}):\n{body}");
        }

        string sessionCookie = ExtractCookieValue(postResponse, SessionCookieName)
            ?? throw new InvalidOperationException("Login succeeded but issued no session cookie.");
        // The antiforgery cookie/token pair is independent of the session cookie (it is not tied to
        // authentication state), and login does not reissue it, so the pre-login pair captured from
        // the Login GET above remains valid for authenticated requests against role-gated endpoints.
        // (A logged-in GET request can't be used to mint a "fresher" token instead: the seeded test
        // menu only contains Anonymous-role categories, so any authenticated GET redirects via
        // AuthorizationFilter's menu-match fallback rather than rendering a page.)
        string antiforgeryCookie = ExtractCookieValue(postResponse, AntiforgeryCookieName) ?? loginAntiforgeryCookie;

        return new AuthenticatedSession(sessionCookie, antiforgeryCookie, loginToken, nickname);
    }

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
        if (!match.Success)
            throw new InvalidOperationException("Could not find __RequestVerificationToken in the response HTML.");
        return match.Groups[1].Value;
    }
}
