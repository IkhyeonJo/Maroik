using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Tokens;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.Website.Controllers;

/// <summary>
/// Handles all self-service account flows: consent, login, logout, registration,
/// email confirmation, forgot-password, and password reset.
/// </summary>
public class AccountController : Controller
{
    private readonly IHtmlLocalizer<AccountController> _localizer;
    private readonly IAccountService _accountService;
    private readonly ISessionService _sessionService;

    /// <summary>Initializes a new instance of <see cref="AccountController"/> with the supplied dependencies.</summary>
    public AccountController(
        IHtmlLocalizer<AccountController> localizer,
        IAccountService accountService,
        ISessionService sessionService)
    {
        _localizer = localizer;
        _accountService = accountService;
        _sessionService = sessionService;
    }

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
    public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel, CancellationToken ct)
    {
        string userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident"))
        {
            TempData["Error"] = _localizer["BlockInternetExplorer"].Value;
            return View();
        }

        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (!ModelState.IsValid) return View();

        var result = await _accountService.LoginAsync(loginInputViewModel.Email!, loginInputViewModel.Password ?? "", ct);

        if (!result.Success)
        {
            TempData["Error"] = _localizer[result.ErrorKey].Value;
            return View();
        }

        // Session fixation mitigation: mint a fresh, unpredictable session id before attaching the
        // account to it, so a session id an attacker fixated before login never becomes authenticated.
        HttpContext.RegenerateSession();
        _sessionService.SetAccount(result.Account!);
        // The new session created by RegenerateSession() above isn't the one SessionMiddleware
        // auto-commits at the end of the request, so it must be saved explicitly here.
        await HttpContext.Session.CommitAsync(ct);
        return RedirectToAction("AnonymousIndex", "Dashboard");
    }
    #endregion

    #region Logout
    /// <summary>Clears the session and redirects to the anonymous dashboard.</summary>
    [HttpGet]
    public IActionResult Logout()
    {
        _sessionService.RemoveAccount();
        return RedirectToAction("AnonymousIndex", "Dashboard");
    }
    #endregion

    #region Register
    /// <summary>Displays the registration form (GET) or processes new-account / resend-email requests (POST).</summary>
    [HttpGet]
    public IActionResult Register() => View();

    /// <summary>Displays the registration form (GET) or processes new-account / resend-email requests (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(loginInputViewModel.Password)) // Registration path
        {
            if (!ModelState.IsValid) return RedirectToAction("Register", "Account");

            if (!loginInputViewModel.AgreedServiceTerms)
            {
                TempData["Error"] = _localizer["Please check the consent form"].Value;
                return View();
            }

            if (string.IsNullOrEmpty(loginInputViewModel.TimeZoneIanaId))
            {
                TempData["Error"] = _localizer["Please select time zone"].Value;
                return View();
            }

            string emailSubject = _localizer["Maroik Email Confirmation"].Value;
            string emailTitle = _localizer["Welcome to Maroik"].Value;
            string emailContent0 = _localizer["Click the link below to verify your Email"].Value;
            string emailContent1 = _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

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

            var result = await _accountService.RegisterAsync(newAccount, emailSubject, emailTitle, emailContent0, emailContent1, ct);

            if (!result.Success)
            {
                TempData["Error"] = _localizer[result.ErrorKey].Value;
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

            string emailSubject = _localizer["Maroik Email Confirmation"].Value;
            string emailTitle = _localizer["Welcome to Maroik"].Value;
            string emailContent0 = _localizer["Click the link below to verify your Email"].Value;
            string emailContent1 = _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

            var result = await _accountService.ResendConfirmationEmailAsync(loginInputViewModel.Email!, emailSubject, emailTitle, emailContent0, emailContent1, ct);

            if (!result.Success)
            {
                TempData["Error"] = _localizer[result.ErrorKey].Value;
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
    public async Task<IActionResult> ConfirmEmail(string registrationToken, CancellationToken ct)
    {
        var result = await _accountService.ConfirmEmailAsync(registrationToken, ct);

        if (!string.IsNullOrEmpty(result.ErrorKey))
            TempData["Error"] = _localizer[result.ErrorKey].Value;

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
    public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel, CancellationToken ct)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ForgotPassword", "Account");

        string emailSubject = _localizer["Maroik Reset Password"].Value;
        string emailTitle = _localizer["Welcome to Maroik"].Value;
        string emailContent0 = _localizer["Click the link below to reset your password"].Value;
        string emailContent1 = _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value;

        await _accountService.ForgotPasswordAsync(loginInputViewModel.Email!, emailSubject, emailTitle, emailContent0, emailContent1, ct);

        ViewBag.MailSent = true;
        return View();
    }

    /// <summary>Validates the reset token (GET) or applies the new password (POST).</summary>
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string resetPasswordToken, CancellationToken ct)
    {
        var result = await _accountService.ValidateResetPasswordTokenAsync(resetPasswordToken, ct);
        ViewBag.FailToResetPassword = result.FailToReset;
        ViewBag.ResetPasswordToken = result.ResetPasswordToken;
        return View();
    }

    /// <summary>Validates the reset token (GET) or applies the new password (POST).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(LoginInputViewModel loginInputViewModel, CancellationToken ct)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Email));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ResetPassword", "Account");

        var result = await _accountService.ResetPasswordAsync(
            loginInputViewModel.ResetPasswordToken ?? "",
            loginInputViewModel.Password ?? "",
            ct);

        if (!result.Success)
        {
            if (result.ErrorKey == "reset-password-invalid")
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            TempData["Error"] = _localizer[result.ErrorKey].Value;
            ViewBag.FailToResetPassword = true;
            return View();
        }

        ViewBag.ResetPasswordComplete = true;
        return View();
    }
    #endregion
}
