using Maroik.WebSite.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>AccountController</c>.
/// All self-service account flows (consent, login, register, forgot-password, etc.)
/// are accessible without authentication and should respond with 200 OK.
/// </summary>
[Collection("WebSite Integration")]
public class AccountControllerTests
{
    private readonly HttpClient _client;

    public AccountControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    // -- ConsentForm ----------------------------------------------------------

    /// <summary>Verifies that <c>ConsentForm</c> get when returns200.</summary>
    [Fact]
    public async Task ConsentForm_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/ConsentForm", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- Login ----------------------------------------------------------------

    /// <summary>Verifies that <c>Login</c> get when returns200.</summary>
    [Fact]
    public async Task Login_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/Login", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- Logout ---------------------------------------------------------------

    /// <summary>Verifies that <c>Logout</c> get when redirects to dash board.</summary>
    [Fact]
    public async Task Logout_Get_RedirectsToDashboard()
    {
        var response = await _client.GetAsync("/Account/Logout", TestContext.Current.CancellationToken);

        // Logout clears session and redirects to "/" (default route resolves Dashboard/AnonymousIndex)
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    // -- Register -------------------------------------------------------------

    /// <summary>Verifies that <c>Register</c> get when returns200.</summary>
    [Fact]
    public async Task Register_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/Register", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- ConfirmEmail ---------------------------------------------------------

    /// <summary>Verifies that <c>ConfirmEmail</c> get when returns 200 with invalid token.</summary>
    [Fact]
    public async Task ConfirmEmail_Get_Returns200_WithInvalidToken()
    {
        // Providing an invalid/fake token; the controller should handle decryption failure
        // and show the view with InvalidToken = true rather than throwing.
        var response = await _client.GetAsync("/Account/ConfirmEmail?registrationToken=invalid-token", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- ForgotPassword -------------------------------------------------------

    /// <summary>Verifies that <c>ForgotPassword</c> get when returns200.</summary>
    [Fact]
    public async Task ForgotPassword_Get_Returns200()
    {
        var response = await _client.GetAsync("/Account/ForgotPassword", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    // -- ResetPassword --------------------------------------------------------

    /// <summary>Verifies that <c>ResetPassword</c> get when returns 200 with invalid token.</summary>
    [Fact]
    public async Task ResetPassword_Get_Returns200_WithInvalidToken()
    {
        // Providing an invalid/fake token; ValidateResetPasswordTokenAsync should catch
        // the decryption failure and show the view with FailToReset = true.
        var response = await _client.GetAsync("/Account/ResetPassword?resetPasswordToken=invalid-token", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
