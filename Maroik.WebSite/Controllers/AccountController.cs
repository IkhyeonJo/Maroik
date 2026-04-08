using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Handles all self-service account flows: consent, login, logout, registration,
/// email confirmation, forgot-password, and password reset.
/// </summary>
public class AccountController(
    IHtmlLocalizer<AccountController> localizer,
    IAccountService accountService,
    IDashboardService dashboardService,
    IOptions<ServerSetting> serverSettings,
    ISessionService sessionService)
    : BaseController(accountService, dashboardService, serverSettings, sessionService)
{
    private readonly IAccountService _accountService = accountService;
    private readonly ISessionService _sessionService = sessionService;

    #region ConsentForm
    /// <summary>Displays the service consent form.</summary>
    [HttpGet]
    public IActionResult ConsentForm() => View();
    #endregion

    #region Login
    /// <summary>Displays the login form (GET) or processes login credentials (POST).</summary>
    [HttpGet]
    public IActionResult Login() => View();

    /// <summary>Displays the login form (GET) or processes login credentials (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
    {
#pragma warning disable ASP0015
        var userAgent = Request.Headers["User-Agent"].ToString();
#pragma warning restore ASP0015
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident"))
        {
            TempData["Error"] = localizer["BlockInternetExplorer"].Value;
            return View();
        }

        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (!ModelState.IsValid) return View();

        var result = await _accountService.LoginAsync(loginInputViewModel.Email, loginInputViewModel.Password ?? "");

        if (!result.Success)
        {
            TempData["Error"] = localizer[result.ErrorKey].Value;
            return View();
        }

        _sessionService.SetAccount(result.Account!);
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }
    #endregion

    #region Logout
    /// <summary>Clears the session and redirects to the anonymous dashboard.</summary>
    [HttpGet]
    public IActionResult Logout()
    {
        _sessionService.RemoveAccount();
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }
    #endregion

    #region Register
    /// <summary>Displays the registration form (GET) or processes new-account / resend-email requests (POST).</summary>
    [HttpGet]
    public IActionResult Register() => View();

    /// <summary>Displays the registration form (GET) or processes new-account / resend-email requests (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel)
    {
        if (!string.IsNullOrEmpty(loginInputViewModel.Password)) // Registration path
        {
            if (!ModelState.IsValid) return RedirectToAction("Register", "Account");

            if (!loginInputViewModel.AgreedServiceTerms)
            {
                TempData["Error"] = localizer["Please check the consent form"].Value;
                return View();
            }

            if (string.IsNullOrEmpty(loginInputViewModel.TimeZoneIanaId))
            {
                TempData["Error"] = localizer["Please select time zone"].Value;
                return View();
            }

            var emailSubject = localizer["Maroik Email Confirmation"].Value;
            var emailTitle = localizer["Welcome to Maroik"].Value;
            var emailContent0 = localizer["Click the link below to verify your Email"].Value;
            var emailContent1 = localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

            var newAccount = new AccountRequest
            {
                Email = loginInputViewModel.Email,
                HashedPassword = loginInputViewModel.Password, // service will hash
                Nickname = loginInputViewModel.Nickname,
                AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                Role = Role.User,
                TimeZoneIanaId = loginInputViewModel.TimeZoneIanaId,
                Locked = false,
                LoginAttempt = 0,
                EmailConfirmed = false,
                AgreedServiceTerms = true,
                RegistrationToken = GuidToken.Generate(),
                ResetPasswordToken = null,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Message = EnumHelper.GetDescription(AccountMessage.UserCreatedVerifyEmail),
                Deleted = false
            };

            var result = await _accountService.RegisterAsync(newAccount, emailSubject, emailTitle, emailContent0, emailContent1);

            if (!result.Success)
            {
                TempData["Error"] = localizer[result.ErrorKey].Value;
                ViewBag.ResendEmail = result.ShowResendEmail;
                return View();
            }

            ViewBag.ResendEmail = result.ShowResendEmail;
            ViewBag.ResentEmailAddress = result.EmailAddress;
        }
        else // Email resend path
        {
            _ = ModelState.Remove(nameof(loginInputViewModel.Password));
            _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

            if (!ModelState.IsValid) return RedirectToAction("Register", "Account");

            var emailSubject = localizer["Maroik Email Confirmation"].Value;
            var emailTitle = localizer["Welcome to Maroik"].Value;
            var emailContent0 = localizer["Click the link below to verify your Email"].Value;
            var emailContent1 = localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

            var result = await _accountService.ResendConfirmationEmailAsync(loginInputViewModel.Email, emailSubject, emailTitle, emailContent0, emailContent1);

            if (!result.Success)
            {
                TempData["Error"] = localizer[result.ErrorKey].Value;
                ViewBag.ResendEmail = result.ShowResendEmail;
                return View();
            }

            ViewBag.ResendEmail = result.ShowResendEmail;
            ViewBag.ResentEmailAddress = result.EmailAddress;
            ViewBag.RepeatEmailSend = result.RepeatEmailSend;
        }

        return View();
    }
    #endregion

    #region ConfirmEmail
    /// <summary>Validates the email-confirmation token from the registration link and activates the account.</summary>
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string registrationToken)
    {
        var result = await _accountService.ConfirmEmailAsync(registrationToken);

        if (!string.IsNullOrEmpty(result.ErrorKey))
            TempData["Error"] = localizer[result.ErrorKey].Value;

        ViewBag.InvalidToken = result.InvalidToken;
        ViewBag.AccountCreated = result.AccountCreated;
        return View();
    }
    #endregion

    #region ForgotPassword
    /// <summary>Displays the forgot-password form (GET) or sends a reset-password email (POST).</summary>
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    /// <summary>Displays the forgot-password form (GET) or sends a reset-password email (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ForgotPassword", "Account");

        var emailSubject = localizer["Maroik Reset Password"].Value;
        var emailTitle = localizer["Welcome to Maroik"].Value;
        var emailContent0 = localizer["Click the link below to reset your password"].Value;
        var emailContent1 = localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

        await _accountService.ForgotPasswordAsync(loginInputViewModel.Email, emailSubject, emailTitle, emailContent0, emailContent1);

        ViewBag.mailSent = true;
        ViewBag.sentEmailAddress = loginInputViewModel.Email;
        return View();
    }

    /// <summary>Validates the reset token (GET) or applies the new password (POST).</summary>
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string resetPasswordToken)
    {
        var result = await _accountService.ValidateResetPasswordTokenAsync(resetPasswordToken);
        ViewBag.FailToResetPassword = result.FailToReset;
        ViewBag.ResetPasswordToken = result.ResetPasswordToken;
        return View();
    }

    /// <summary>Validates the reset token (GET) or applies the new password (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Email));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ResetPassword", "Account");

        var result = await _accountService.ResetPasswordAsync(
            loginInputViewModel.ResetPasswordToken ?? "",
            loginInputViewModel.Password ?? "");

        if (!result.Success)
        {
            if (result.ErrorKey == "reset-password-invalid")
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            TempData["Error"] = localizer[result.ErrorKey].Value;
            ViewBag.FailToResetPassword = true;
            return View();
        }

        ViewBag.resetPasswordComplete = true;
        return View();
    }
    #endregion

}
