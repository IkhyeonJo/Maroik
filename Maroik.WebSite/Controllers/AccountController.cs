using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class AccountController : BaseController
{
    private readonly IHtmlLocalizer<AccountController> _localizer;
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountService _accountService;
    private readonly IMailClient _mailClient;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ISessionService _sessionService;

    public AccountController(
        IHtmlLocalizer<AccountController> localizer,
        ILogger<AccountController> logger,
        IAccountService accountService,
        IMailClient mailClient,
        IOptions<ServerSetting> settings,
        ISessionService sessionService)
    {
        _localizer = localizer;
        _logger = logger;
        _accountService = accountService;
        _mailClient = mailClient;
        _settings = settings;
        _sessionService = sessionService;
    }

    #region ConsentForm
    [HttpGet]
    public IActionResult ConsentForm() => View();
    #endregion

    #region Login
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
    {
        string userAgent = Request.Headers["User-Agent"].ToString();
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident"))
        {
            TempData["Error"] = _localizer["BlockInternetExplorer"].Value;
            return View();
        }

        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (!ModelState.IsValid) return View();

        var result = await _accountService.LoginAsync(loginInputViewModel.Email, loginInputViewModel.Password ?? "");

        if (!result.Success)
        {
            TempData["Error"] = _localizer[result.ErrorKey].Value;
            return View();
        }

        _sessionService.SetAccount(result.Account!);
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }
    #endregion

    #region Logout
    [HttpGet]
    public IActionResult Logout()
    {
        _sessionService.RemoveAccount();
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }
    #endregion

    #region Register
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel)
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
            Func<string, string> emailBodyFactory = signedToken => _mailClient.GetMailConfirmationBody(
                signedToken,
                _localizer["Welcome to Maroik"].Value,
                _localizer["Click the link below to verify your Email"].Value,
                _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value,
                _settings.Value.DomainName ?? string.Empty);

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
                RegistrationToken = GUIDToken.Generate(),
                ResetPasswordToken = null,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Message = EnumHelper.GetDescription(AccountMessage.UserCreatedVerifyEmail),
                Deleted = false
            };

            var result = await _accountService.RegisterAsync(newAccount, emailSubject, emailBodyFactory);

            if (!result.Success)
            {
                TempData["Error"] = _localizer[result.ErrorKey].Value;
                ViewBag.ResendEmail = result.ShowResendEmail;
                return View();
            }

            ViewBag.ResendEmail = result.ShowResendEmail;
            ViewBag.ResentEmailAddress = result.EmailAddress;
            return View();
        }
        else // Email resend path
        {
            _ = ModelState.Remove(nameof(loginInputViewModel.Password));
            _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

            if (!ModelState.IsValid) return RedirectToAction("Register", "Account");

            string emailSubject = _localizer["Maroik Email Confirmation"].Value;
            Func<string, string> emailBodyFactory = signedToken => _mailClient.GetMailConfirmationBody(
                signedToken,
                _localizer["Welcome to Maroik"].Value,
                _localizer["Click the link below to verify your Email"].Value,
                _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value,
                _settings.Value.DomainName ?? string.Empty);

            var result = await _accountService.ResendConfirmationEmailAsync(loginInputViewModel.Email, emailSubject, emailBodyFactory);

            if (!result.Success)
            {
                TempData["Error"] = _localizer[result.ErrorKey].Value;
                ViewBag.ResendEmail = result.ShowResendEmail;
                return View();
            }

            ViewBag.ResendEmail = result.ShowResendEmail;
            ViewBag.ResentEmailAddress = result.EmailAddress;
            ViewBag.RepeatEmailSend = result.RepeatEmailSend;
            return View();
        }
    }
    #endregion

    #region ConfirmEmail
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string registrationToken)
    {
        var result = await _accountService.ConfirmEmailAsync(registrationToken);

        if (!string.IsNullOrEmpty(result.ErrorKey))
            TempData["Error"] = _localizer[result.ErrorKey].Value;

        ViewBag.InvalidToken = result.InvalidToken;
        ViewBag.AccountCreated = result.AccountCreated;
        return View();
    }
    #endregion

    #region ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ForgotPassword", "Account");

        string emailSubject = _localizer["Maroik Reset Password"].Value;

        await _accountService.ForgotPasswordAsync(
            loginInputViewModel.Email,
            emailSubject,
            signedToken => _mailClient.GetMailResetPasswordBody(
                signedToken,
                _localizer["Welcome to Maroik"].Value,
                _localizer["Click the link below to reset your password"].Value,
                _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value,
                _settings.Value.DomainName ?? string.Empty));

        ViewBag.mailSent = true;
        ViewBag.sentEmailAddress = loginInputViewModel.Email;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string resetPasswordToken)
    {
        var result = await _accountService.ValidateResetPasswordTokenAsync(resetPasswordToken);
        ViewBag.FailToResetPassword = result.FailToReset;
        ViewBag.ResetPasswordToken = result.ResetPasswordToken;
        return View();
    }

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
            TempData["Error"] = _localizer[result.ErrorKey].Value;
            ViewBag.FailToResetPassword = true;
            return View();
        }

        ViewBag.resetPasswordComplete = true;
        return View();
    }
    #endregion

}
