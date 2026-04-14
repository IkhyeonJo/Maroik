using Maroik.WebSite.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>AccountController</c>.
/// All self-service account flows (consent, login, register, forgot-password, etc.)
/// are accessible without authentication and should respond with 200 OK.
/// </summary>
public class AccountControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // ── ConsentForm ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConsentForm_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/ConsentForm", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/Login", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── Logout ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Get_RedirectsToDashBoard()
    {
        var response = await _client.GetAsync("/Account/Logout", TestContext.Current.CancellationToken);

        // Logout clears session and redirects — no auth required
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("DashBoard", response.Headers.Location?.ToString() ?? "");
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/Register", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── ConfirmEmail ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmail_Get_Returns200_WithInvalidToken()
    {
        // Providing an invalid/fake token; the controller should handle decryption failure
        // and show the view with InvalidToken = true rather than throwing.
        var response = await _client.GetAsync("/Account/ConfirmEmail?registrationToken=invalid-token", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── ForgotPassword ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/ForgotPassword", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // ── ResetPassword ────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_Get_Returns200_WithInvalidToken()
    {
        // Providing an invalid/fake token; ValidateResetPasswordTokenAsync should catch
        // the decryption failure and show the view with FailToReset = true.
        var response = await _client.GetAsync("/Account/ResetPassword?resetPasswordToken=invalid-token", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
