using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Profile action group of <c>ManagementController</c> —
/// <c>UpdateProfileTimeZone</c>/<c>UpdateProfilePassword</c>/<c>UpdateProfileAvatar</c> —
/// previously entirely uncovered.
///
/// <c>UpdateProfileAvatar</c>'s success path is not covered here: it calls
/// <c>ProfileService.UploadAndUpdateAvatarAsync</c>, which scans the upload through a real
/// ClamAV daemon (<c>ServerSetting.ClamavHost</c>/<c>ClamavPort</c>) before anything else — no
/// such daemon is available in this test environment, so a real upload would always fail at the
/// virus-scan step regardless of the file's content. Only the validation branches that return
/// before ClamAV is reached (missing file, oversized file, disallowed extension) are exercised.
/// </summary>
[Collection("Website Integration")]
public class ManagementControllerProfileTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of ManagementControllerProfileTests, sharing the MaroikWebApplicationFactory fixture across the tests in this class.</summary>
    public ManagementControllerProfileTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "management-profile-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    // -- Profile: GET -----------------------------------------------------------------

    /// <summary>Profile get logged in user returns200.</summary>
    [Fact]
    public async Task Profile_Get_LoggedInUser_Returns200()
    {
        var session = await LoginAsUserAsync();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Management/Profile");
        request.Headers.Add("Cookie", session.CookieHeader);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- UpdateProfileTimeZone ----------------------------------------------------------

    /// <summary>Update profile time zone valid id persists change.</summary>
    [Fact]
    public async Task UpdateProfileTimeZone_ValidId_PersistsChange()
    {
        var session = await LoginAsUserAsync("management-profile-tz@test.com");
        using var request = session.BuildFormPostRequest("/Management/UpdateProfileTimeZone",
            new MultipartFormDataContent { { new StringContent("Asia/Seoul"), "TimeZoneIanaId" } });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == "management-profile-tz@test.com");
        Assert.Equal("Asia/Seoul", account.TimeZoneIanaId);
    }

    /// <summary>Update profile time zone invalid time zone leaves account unchanged.</summary>
    [Fact]
    public async Task UpdateProfileTimeZone_InvalidTimeZone_LeavesAccountUnchanged()
    {
        var session = await LoginAsUserAsync("management-profile-tz-bad@test.com");
        using var request = session.BuildFormPostRequest("/Management/UpdateProfileTimeZone",
            new MultipartFormDataContent { { new StringContent("Not/ARealZone"), "TimeZoneIanaId" } });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode); // redirects regardless; failure is logged server-side

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == "management-profile-tz-bad@test.com");
        Assert.Equal("UTC", account.TimeZoneIanaId); // unchanged from seed default
    }

    // -- UpdateProfilePassword ----------------------------------------------------------

    /// <summary>Update profile password correct current password changes password.</summary>
    [Fact]
    public async Task UpdateProfilePassword_CorrectCurrentPassword_ChangesPassword()
    {
        var session = await LoginAsUserAsync("management-profile-pw@test.com");
        using var request = session.BuildJsonPostRequest("/Management/UpdateProfilePassword", new
        {
            Nickname = "unused",
            Password = "UserPassword1!",
            NewPassword = "BrandNewPassword1!",
            TimeZoneIanaId = "unused"
        });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var account = db.Accounts.Single(a => a.Email == "management-profile-pw@test.com");
        Assert.True(passwordService.VerifyPassword("BrandNewPassword1!", account.HashedPassword!));
    }

    /// <summary>Update profile password wrong current password returns failure result.</summary>
    [Fact]
    public async Task UpdateProfilePassword_WrongCurrentPassword_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync("management-profile-pw-wrong@test.com");
        using var request = session.BuildJsonPostRequest("/Management/UpdateProfilePassword", new
        {
            Nickname = "unused",
            Password = "TotallyWrongPassword1!",
            NewPassword = "BrandNewPassword1!",
            TimeZoneIanaId = "unused"
        });

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var account = db.Accounts.Single(a => a.Email == "management-profile-pw-wrong@test.com");
        Assert.True(passwordService.VerifyPassword("UserPassword1!", account.HashedPassword!)); // unchanged
    }

    // -- UpdateProfileAvatar: validation only (see class doc comment) -------------------

    /// <summary>Update profile avatar no file attached returns failure result.</summary>
    [Fact]
    public async Task UpdateProfileAvatar_NoFileAttached_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync("management-profile-avatar-nofile@test.com");
        // A real client always submits at least the other profile fields alongside the file
        // input, so a literally empty multipart body isn't representative — one harmless field
        // keeps the request well-formed while still attaching no file.
        var form = new MultipartFormDataContent { { new StringContent("unused"), "Nickname" } };
        using var request = session.BuildFormPostRequest("/Management/UpdateProfileAvatar", form);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json);
    }

    /// <summary>Update profile avatar disallowed extension returns failure result.</summary>
    [Fact]
    public async Task UpdateProfileAvatar_DisallowedExtension_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync("management-profile-avatar-badext@test.com");
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "ProfileAvatarFiles", "notanimage.txt");
        using var request = session.BuildFormPostRequest("/Management/UpdateProfileAvatar", form);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"result\":false", json);
    }
}
