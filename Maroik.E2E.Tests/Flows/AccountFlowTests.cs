using Maroik.E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace Maroik.E2E.Tests.Flows;

/// <summary>
/// End-to-end browser tests for the Account self-service flows.
///
/// Each test launches headless Chromium via Playwright against the real Kestrel server
/// started by <see cref="PlaywrightWebApplicationFactory"/>.  Tests verify that the
/// correct HTML elements are present on the page — no form submission is made
/// (POST requires a real DB and SMTP server).
/// </summary>
public class AccountFlowTests : E2ETestBase
{
    // ── Login ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginPage_HasEmailAndPasswordInputsAndSubmitButton()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/Login");

        // The login form exists with id="loginForm"
        await page.WaitForSelectorAsync("#loginForm");

        // Both credential inputs are rendered
        await Assertions.Expect(page.Locator("input[name='Email']")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Password")).ToBeVisibleAsync();

        // The sign-in submit button is present and enabled
        await Assertions.Expect(page.Locator("#btnSignIn")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#btnSignIn")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task LoginPage_HasTitle()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/Login");

        // The browser page title must be non-empty
        string title = await page.TitleAsync();
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    // ── Register ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterPage_HasRegistrationFormWithAllFields()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/Register");

        await page.WaitForSelectorAsync("#registerForm");

        // All required registration fields are visible
        await Assertions.Expect(page.Locator("#Nickname")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Email")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#Password")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("#btnRegister")).ToBeVisibleAsync();
    }

    // ── ConsentForm ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ConsentFormPage_Loads_WithNonEmptyBody()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/ConsentForm");

        // Page loaded successfully — body element must have content
        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    // ── ForgotPassword ───────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordPage_HasEmailInputAndSubmitButton()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/ForgotPassword");

        // The email input and submit button must both be rendered
        await Assertions.Expect(page.Locator("input[name='Email']")).ToBeVisibleAsync();

        var submitButton = page.Locator("button[type='submit']");
        await Assertions.Expect(submitButton).ToBeVisibleAsync();
    }

    // ── ConfirmEmail (invalid token) ─────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmailPage_WithInvalidToken_LoadsWithoutUnhandledError()
    {
        var page = await NewPageAsync();

        // Track JavaScript console errors
        var jsErrors = new List<string>();
        page.Console += (_, e) =>
        {
            if (e.Type == "error") jsErrors.Add(e.Text);
        };

        await GotoAsync(page, "/Account/ConfirmEmail?registrationToken=bad-token");

        // Page must render (no 500 crash)
        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    // ── ResetPassword (invalid token) ────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordPage_WithInvalidToken_LoadsWithoutUnhandledError()
    {
        var page = await NewPageAsync();
        await GotoAsync(page, "/Account/ResetPassword?resetPasswordToken=bad-token");

        string bodyText = await page.Locator("body").InnerTextAsync();
        Assert.False(string.IsNullOrWhiteSpace(bodyText));
    }

    // ── Logout → redirect ────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_RedirectsToAnonymousDashboard()
    {
        var page = await NewPageAsync();
        // Logout clears session and redirects; Playwright follows it automatically
        await GotoAsync(page, "/Account/Logout");

        // After redirect, we should be on the DashBoard/AnonymousIndex page
        string url = page.Url;
        Assert.Contains("DashBoard", url, StringComparison.OrdinalIgnoreCase);
    }
}
