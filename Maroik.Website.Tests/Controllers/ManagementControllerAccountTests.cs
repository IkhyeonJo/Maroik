using Maroik.Core.Contract.Constants;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Website.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the Account CRUD action group of <c>ManagementController</c>
/// (create/read/update/delete, all <c>[RequiredHttpPostAccess(Role = Role.Admin)]</c>-gated).
/// Drives a real authenticated session via <see cref="AuthenticatedSessionHelper"/> so the
/// inline validation and role-gating logic in the controller is exercised end-to-end.
///
/// Note on <c>CreateAccount</c>'s DB-write success path: <c>Account.Create()</c> deliberately
/// leaves <c>AvatarImagePath</c> unset, relying on the column's <c>HasDefaultValueSql</c> in
/// PostgreSQL to fill it in on insert. The EF Core InMemory provider used by this test factory
/// does not evaluate SQL defaults, so it throws "required properties are missing" for that one
/// property — a InMemory-provider limitation, not an application bug (confirmed by reproducing
/// the same call directly against <c>IAccountRepository</c>, bypassing the controller entirely).
/// Accordingly, tests here that need an existing account seed it directly (as every other
/// integration test in this suite already does) rather than routing through the real
/// <c>CreateAccount</c> endpoint.
/// </summary>
[Collection("Website Integration")]
public class ManagementControllerAccountTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ManagementControllerAccountTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsAdminAsync(string email = "mgmt-admin@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "mgmt-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private void SeedAccount(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (db.Accounts.Any(a => a.Email == email)) return;
        db.Accounts.Add(new Account
        {
            Email = email,
            HashedPassword = "irrelevant-hash",
            Nickname = $"Seed-{Guid.NewGuid():N}",
            AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
            Role = Role.User,
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

    private static object ValidAccountPayload(string email) => new
    {
        Email = email,
        Password = "NewAccountPw1!",
        Nickname = "Newcomer",
        AvatarImagePath = "",
        Role = Role.User,
        TimeZoneIanaId = "UTC",
        Locked = false,
        LoginAttempt = 0,
        EmailConfirmed = true,
        AgreedServiceTerms = true,
        RegistrationToken = "",
        ResetPasswordToken = "",
        Message = "",
        Deleted = false
    };

    // -- CreateAccount: role gating --------------------------------------------

    [Fact]
    public async Task CreateAccount_UserSession_IsForbidden()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/Management/CreateAccount", ValidAccountPayload("blocked@test.com"));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- CreateAccount: inline validation ---------------------------------------

    [Fact]
    public async Task CreateAccount_InvalidEmailFormat_ReturnsSpecificError()
    {
        var session = await LoginAsAdminAsync();
        using var request = session.BuildJsonPostRequest("/Management/CreateAccount", ValidAccountPayload("not-an-email"));

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Invalid email form", json);
    }

    [Fact]
    public async Task CreateAccount_WeakPassword_ReturnsPasswordPolicyError()
    {
        var session = await LoginAsAdminAsync();
        var payload = new
        {
            Email = "weakpass@test.com",
            Password = "weak",
            Nickname = "Weak",
            AvatarImagePath = "",
            Role = Role.User,
            TimeZoneIanaId = "UTC",
            Locked = false,
            LoginAttempt = 0,
            EmailConfirmed = true,
            AgreedServiceTerms = true,
            RegistrationToken = "",
            ResetPasswordToken = "",
            Message = "",
            Deleted = false
        };
        using var request = session.BuildJsonPostRequest("/Management/CreateAccount", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Password must be at least 8 characters", json);
    }

    [Fact]
    public async Task CreateAccount_MissingTimeZone_ReturnsSpecificError()
    {
        var session = await LoginAsAdminAsync();
        var payload = new
        {
            Email = "notz@test.com",
            Password = "ValidPassw0rd!",
            Nickname = "NoTz",
            AvatarImagePath = "",
            Role = Role.User,
            TimeZoneIanaId = "",
            Locked = false,
            LoginAttempt = 0,
            EmailConfirmed = true,
            AgreedServiceTerms = true,
            RegistrationToken = "",
            ResetPasswordToken = "",
            Message = "",
            Deleted = false
        };
        using var request = session.BuildJsonPostRequest("/Management/CreateAccount", payload);

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Please select time zone", json);
    }

    // -- IsAccountExists ---------------------------------------------------------

    [Fact]
    public async Task IsAccountExists_SeededAccount_ReturnsSuccessResult()
    {
        var session = await LoginAsAdminAsync();
        const string email = "existing-account@test.com";
        SeedAccount(email);

        using var request = session.BuildJsonPostRequest($"/Management/IsAccountExists?email={email}");
        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    [Fact]
    public async Task IsAccountExists_UnknownEmail_ReturnsFailureResult()
    {
        var session = await LoginAsAdminAsync();
        using var request = session.BuildJsonPostRequest("/Management/IsAccountExists?email=nobody-xyz@test.com");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Fail to find the account by given email address", json);
    }

    // -- UpdateAccount ------------------------------------------------------------

    [Fact]
    public async Task UpdateAccount_ExistingAccount_ReturnsSuccessResult()
    {
        var session = await LoginAsAdminAsync();
        const string email = "to-update@test.com";
        SeedAccount(email);

        using var updateRequest = session.BuildJsonPostRequest("/Management/UpdateAccount", new
        {
            Email = email,
            Password = "UnchangedPassw0rd!",
            Role = Role.User,
            TimeZoneIanaId = "Asia/Seoul",
            Locked = true,
            EmailConfirmed = true,
            AgreedServiceTerms = true,
            Message = "updated",
            Deleted = false
        });
        var response = await _client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    /// <summary>
    /// Regression test: an empty <c>Password</c> must mean "leave the current password
    /// unchanged", matching the admin edit form's real behavior (it submits whatever is
    /// currently in the password field, which is blank unless the admin typed a new one).
    /// Previously blocked by <c>[Required]</c> on <c>AccountInputViewModel.Password</c>, which
    /// made it impossible to edit any other field (Role, Locked, etc.) without also being forced
    /// to set a new password.
    /// </summary>
    [Fact]
    public async Task UpdateAccount_BlankPasswordAndMessage_StillSucceeds()
    {
        var session = await LoginAsAdminAsync();
        const string email = "blank-password-update@test.com";
        SeedAccount(email);

        using var updateRequest = session.BuildJsonPostRequest("/Management/UpdateAccount", new
        {
            Email = email,
            Password = "",
            Role = Role.Admin,
            TimeZoneIanaId = "Asia/Seoul",
            Locked = true,
            EmailConfirmed = true,
            AgreedServiceTerms = true,
            Message = "",
            Deleted = false
        });
        var response = await _client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.Equal(Role.Admin, account.Role);
        Assert.True(account.Locked);
        Assert.Equal("irrelevant-hash", account.HashedPassword); // unchanged from SeedAccount
    }

    [Fact]
    public async Task UpdateAccount_UnknownEmail_ReturnsFailureResult()
    {
        var session = await LoginAsAdminAsync();

        using var updateRequest = session.BuildJsonPostRequest("/Management/UpdateAccount", new
        {
            Email = "does-not-exist@test.com",
            Password = "UnchangedPassw0rd!",
            Role = Role.User,
            TimeZoneIanaId = "UTC",
            Locked = false,
            EmailConfirmed = true,
            AgreedServiceTerms = true,
            Message = "none",
            Deleted = false
        });
        var response = await _client.SendAsync(updateRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Email address is wrong", json);
    }

    // -- DeleteAccount ------------------------------------------------------------

    [Fact]
    public async Task DeleteAccount_ExistingAccount_SoftDeletesIt()
    {
        var session = await LoginAsAdminAsync();
        const string email = "to-delete@test.com";
        SeedAccount(email);

        using var deleteRequest = session.BuildJsonPostRequest("/Management/DeleteAccount", new { Email = email });
        var deleteResponse = await _client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);
        string deleteJson = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", deleteJson);

        // DeleteAccount is a soft delete (Deleted flag flipped), not a row removal — FindByEmailAsync
        // does not filter on Deleted, so verify the flag directly rather than via IsAccountExists.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = db.Accounts.Single(a => a.Email == email);
        Assert.True(account.Deleted);
    }

    [Fact]
    public async Task DeleteAccount_UnknownEmail_ReturnsFailureResult()
    {
        var session = await LoginAsAdminAsync();

        using var deleteRequest = session.BuildJsonPostRequest("/Management/DeleteAccount", new { Email = "nobody-to-delete@test.com" });
        var response = await _client.SendAsync(deleteRequest, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("Fail to find the account by given email address", json);
    }
}
