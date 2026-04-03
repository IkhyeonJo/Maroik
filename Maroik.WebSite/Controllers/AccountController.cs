using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Utilities;
using Maroik.Core.Service.Contracts;
using Maroik.Core.Client.Contracts;
using Maroik.WebSite.Models.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class AccountController : Controller
{
    private readonly RSA _rsa;
    private readonly IHtmlLocalizer<AccountController> _localizer;
    private readonly ILogger<AccountController> _logger;
    private readonly IAccountRepository _accountRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly IMailClient _mailClient;
    private readonly IPasswordService _passwordService;

    public AccountController(IHtmlLocalizer<AccountController> localizer, ILogger<AccountController> logger, IAccountRepository accountRepository, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IMailClient mailClient, IPasswordService passwordService)
    {
        _rsa = new RSA(ServerSetting.RSAAlgorithm, Encoding.UTF8, ServerSetting.RSAPrivateKey, ServerSetting.RSAPublicKey);
        _localizer = localizer;
        _logger = logger;
        _accountRepository = accountRepository;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _mailClient = mailClient;
        _passwordService = passwordService;
    }

    #region ConsentForm
    [HttpGet]
    public IActionResult ConsentForm()
    {
        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

        if (isAccountSessionExist) // If logged in
        {
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            if (loginedAccount.Role == Role.Admin) // If logged-in user role is Admin
            {
                return RedirectToAction("AdminIndex", "DashBoard");
            }
            else if (loginedAccount.Role == Role.User) // If logged-in user role is User
            {
                return RedirectToAction("UserIndex", "DashBoard");
            }
            else // If logged-in user role is neither Admin nor User, it's a DB Insert error
            {
                return null; // Wrong input produces wrong output
            }
        }
        else // If not logged in
        {
            return View();
        }
    }
    #endregion

    #region Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
    {
        string userAgent = Request.Headers["User-Agent"].ToString();
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) // IsInternetExplorer (Block reason: Issues occur in Nonfactor Data Grid...)
        {
            TempData["Error"] = _localizer["BlockInternetExplorer"].Value;
            return View();
        }

        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (ModelState.IsValid)
        {
            Account tempAccount = await _accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

            if (tempAccount == null) // no account exist
            {
                TempData["Error"] = _localizer["Email or Password is wrong"].Value;
                return View();
            }

            if (tempAccount.Deleted)
            {
                TempData["Error"] = _localizer["Your Account is Deleted, Please create your new account by clicking Sign up Button"].Value;
                return View();
            }

            if (tempAccount.Locked)
            {
                TempData["Error"] = _localizer["Your Account is Locked, Please reset your password by clicking Forgot password Button"].Value;
                return View();
            }

            if (!_passwordService.VerifyPassword(loginInputViewModel.Password ?? "", tempAccount.HashedPassword)) // Wrong password
            {
                tempAccount.LoginAttempt++;

                if (tempAccount.LoginAttempt == ServerSetting.MaxLoginAttempt)
                {
                    tempAccount.Locked = true;
                    tempAccount.Message = EnumHelper.GetDescription(AccountMessage.AccountLocked);
                    await _accountRepository.UpdateAccountAsync(tempAccount);
                    TempData["Error"] = _localizer["Your Account is Locked, Please reset your password by clicking Forgot password Button"].Value;
                    return View();
                }
                else
                {
                    await _accountRepository.UpdateAccountAsync(tempAccount);
                    TempData["Error"] = _localizer["Email or Password is wrong"].Value;
                    return View();
                }
            }

            if (tempAccount.EmailConfirmed == true && tempAccount.AgreedServiceTerms == true)
            {
                tempAccount.LoginAttempt = 0;
                await _accountRepository.UpdateAccountAsync(tempAccount);
                tempAccount.HashedPassword = null;
                HttpContext.Session.Set(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));

                #region DB Cache
                return tempAccount.Role == Role.Admin
                    ? RedirectToAction("AdminIndex", "DashBoard")
                    : tempAccount.Role == Role.User ? RedirectToAction("UserIndex", "DashBoard") : (IActionResult)null;
                #endregion
            }
            else
            {
                if (tempAccount.EmailConfirmed == false)
                {
                    TempData["Error"] = _localizer["Email verification was not completed. Please try sign up again."].Value;
                    return View();
                }
                else if (tempAccount.AgreedServiceTerms == false)
                {
                    TempData["Error"] = _localizer["Agreed Service Terms was not checked. Please try sign up again and login again."].Value;
                    return View();
                }
                else
                {
                    TempData["Error"] = _localizer["Email verification was not completed and Agreed Service Terms was not checked. Please try sign up again."].Value;
                    return View();
                }
            }
        }
        else
        {
            return View();
        }
    }
    #endregion

    #region Logout
    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account));
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }
    #endregion

    #region Register
    [HttpGet]
    public IActionResult Register()
    {
        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

        if (isAccountSessionExist) // If logged in
        {
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            if (loginedAccount.Role == Role.Admin) // If logged-in user has Admin role
            {
                return RedirectToAction("AdminIndex", "DashBoard");
            }
            else if (loginedAccount.Role == Role.User) // If logged-in user has User role
            {
                return RedirectToAction("UserIndex", "DashBoard");
            }
            else // If logged-in user role is neither Admin nor User, it's a DB Insert error
            {
                return null; // Invalid input returns invalid output
            }
        }
        else // If not logged in
        {
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel)
    {
        if (!string.IsNullOrEmpty(loginInputViewModel.Password)) // Registration
        {
            if (ModelState.IsValid) // Input validation
            {
                if (!loginInputViewModel.AgreedServiceTerms) // If consent form not agreed
                {
                    TempData["Error"] = _localizer["Please check the consent form"].Value;
                    return View();
                }

                if (string.IsNullOrEmpty(loginInputViewModel.TimeZoneIanaId)) // If TimeZone not selected
                {
                    TempData["Error"] = _localizer["Please select time zone"].Value;
                    return View();
                }

                Account account = await _accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

                #region Proceeding with new registration
                if (account == null) // No record created with the entered ID
                {
                    string registrationToken = GUIDToken.Generate();
                    loginInputViewModel.RegistrationToken = registrationToken;
                    try
                    {
                        await _accountRepository.CreateAccountAsync
                            (
                            new Account()
                            {
                                Email = loginInputViewModel.Email,
                                HashedPassword = _passwordService.HashPassword(loginInputViewModel.Password),
                                Nickname = loginInputViewModel.Nickname,
                                AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                                Role = Role.User,
                                TimeZoneIanaId = loginInputViewModel.TimeZoneIanaId,
                                Locked = false,
                                LoginAttempt = 0,
                                EmailConfirmed = false,
                                AgreedServiceTerms = true,
                                RegistrationToken = loginInputViewModel.RegistrationToken,
                                ResetPasswordToken = null,
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Message = EnumHelper.GetDescription(AccountMessage.UserCreatedVerifyEmail),
                                Deleted = false
                            }
                            );
                    }
                    catch (DbUpdateException e)
                    {
                        if (((Npgsql.NpgsqlException)e.InnerException).SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
                        {
                            Console.WriteLine(_localizer["Input is invalid"].Value); // Should log this with ActivityLog etc..
                            TempData["Error"] = $"'{loginInputViewModel.Nickname}' {_localizer["is a Nickname that already exists."].Value} {_localizer["Please enter another Nickname."].Value}";
                            return View();
                        }
                        else
                        {
                            Console.WriteLine(_localizer["Input is invalid"].Value); // Should log this with ActivityLog etc..
                            TempData["Error"] = _localizer["Error occurred while processing about account registration"].Value;
                            return View();
                        }
                    }
                    catch
                    {
                        Console.WriteLine(_localizer["Input is invalid"].Value); // Should log this with ActivityLog etc..
                        TempData["Error"] = _localizer["Error occurred while processing about account registration"].Value;
                        return View();
                    }
                    #region Send Email
                    loginInputViewModel.RegistrationToken = _rsa.Sign(loginInputViewModel.RegistrationToken); // Encrypt
                    string result = await _mailClient.SendMailAsync(
                        loginInputViewModel.Email,
                        _localizer["Maroik Email Confirmation"].Value,
                        _mailClient.GetMailConfirmationBody(loginInputViewModel.RegistrationToken, _localizer["Welcome to Maroik"].Value, _localizer["Click the link below to verify your Email"].Value, _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value, ServerSetting.DomainName ?? string.Empty),
                        ServerSetting.SmtpHost ?? string.Empty,
                        ServerSetting.SmtpPort.ToString(),
                        ServerSetting.SmtpUserName ?? string.Empty,
                        ServerSetting.SmtpPassword ?? string.Empty,
                        ServerSetting.SmtpSSL.ToString(),
                        ServerSetting.FromEmail ?? string.Empty,
                        ServerSetting.FromFullName ?? string.Empty);
                    if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // When mail sending error occurs..
                    {
                        Console.WriteLine(result); // Should log this with ActivityLog etc..
                        loginInputViewModel.RegistrationToken = registrationToken;
                        try
                        {
                            await _accountRepository.UpdateAccountAsync
                                (
                                new Account()
                                {
                                    Email = loginInputViewModel.Email,
                                    HashedPassword = _passwordService.HashPassword(loginInputViewModel.Password),
                                    Nickname = loginInputViewModel.Nickname,
                                    AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                                    Role = Role.User,
                                    TimeZoneIanaId = loginInputViewModel.TimeZoneIanaId,
                                    Locked = false,
                                    LoginAttempt = 0,
                                    EmailConfirmed = false,
                                    AgreedServiceTerms = true,
                                    RegistrationToken = loginInputViewModel.RegistrationToken,
                                    ResetPasswordToken = null,
                                    Created = DateTime.UtcNow,
                                    Updated = DateTime.UtcNow,
                                    Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent),
                                    Deleted = false
                                });
                        }
                        catch
                        {
                            TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                            return View();
                        }
                        TempData["Error"] = _localizer["Error occurred while processing about sending account authentication mail"].Value;
                        return View();
                    }
                    #endregion
                    ViewBag.ResendEmail = true; // Enable email resend form
                    ViewBag.ResentEmailAddress = loginInputViewModel.Email; // For displaying email address in email resend form
                    return View();
                }
                #endregion

                #region If registered
                else // Record created with inputted ID exists.
                {
                    #region If email not verified
                    if (!account.EmailConfirmed) // Created but email not verified
                    {
                        #region Send Email
                        if (string.IsNullOrEmpty(account.RegistrationToken))
                        {
                            account.RegistrationToken = GUIDToken.Generate();
                        }
                        string result = await _mailClient.SendMailAsync(
                            account.Email,
                            _localizer["Maroik Email Confirmation"].Value,
                            _mailClient.GetMailConfirmationBody(_rsa.Sign(account.RegistrationToken), _localizer["Welcome to Maroik"].Value, _localizer["Click the link below to verify your Email"].Value, _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value, ServerSetting.DomainName ?? string.Empty),
                            ServerSetting.SmtpHost ?? string.Empty,
                            ServerSetting.SmtpPort.ToString(),
                            ServerSetting.SmtpUserName ?? string.Empty,
                            ServerSetting.SmtpPassword ?? string.Empty,
                            ServerSetting.SmtpSSL.ToString(),
                            ServerSetting.FromEmail ?? string.Empty,
                            ServerSetting.FromFullName ?? string.Empty);
                        if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // When mail sending error occurs..
                        {
                            Console.WriteLine(result); // Should log this with ActivityLog etc..
                            try
                            {
                                account.Updated = DateTime.UtcNow;
                                account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                                await _accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                                return View();
                            }
                            TempData["Error"] = _localizer["Error occurred while processing about sending account authentication mail"].Value;
                            return View();
                        }
                        #endregion
                        try
                        {
                            account.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                            account.Updated = DateTime.UtcNow;
                            await _accountRepository.UpdateAccountAsync(account);
                        }
                        catch
                        {
                            TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                            return View();
                        }
                        ViewBag.ResendEmail = true; // Enable email resend form
                        ViewBag.ResentEmailAddress = account.Email; // For displaying email address in email resend form
                        return View();
                    }
                    #endregion

                    #region If email verified
                    else // Account creation and email verification completed
                    {
                        try
                        {
                            account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
                            account.Updated = DateTime.UtcNow;
                            account.AgreedServiceTerms = true;
                            await _accountRepository.UpdateAccountAsync(account);
                        }
                        catch
                        {
                            TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                            return View();
                        }
                        TempData["Error"] = _localizer[EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated)].Value;
                        ViewBag.ResendEmail = false; // Enable login form
                        return View();
                    }
                    #endregion
                }
                #endregion
            }
            else // Invalid input
            {
                return RedirectToAction("Register", "Account");
            }
        }
        else // Email resend
        {
            _ = ModelState.Remove(nameof(loginInputViewModel.Password));
            _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

            if (ModelState.IsValid) // Input validation
            {
                Account account = await _accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

                #region No record found in DB query with received email when resending email (bug)
                if (account == null) // No record created with inputted ID.
                {
                    TempData["Error"] = _localizer["Failed to resend email"].Value;
                    ViewBag.ResendEmail = true; // Enable email resend form
                    return View();
                }
                #endregion

                #region Email resend
                else // Record created with inputted ID exists.
                {
                    #region Created but email not verified
                    if (!account.EmailConfirmed) // Created but email not verified.
                    {
                        #region Send Email
                        string result = await _mailClient.SendMailAsync(
                            account.Email,
                            _localizer["Maroik Email Confirmation"].Value,
                            _mailClient.GetMailConfirmationBody(_rsa.Sign(account.RegistrationToken), _localizer["Welcome to Maroik"].Value, _localizer["Click the link below to verify your Email"].Value, _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value, ServerSetting.DomainName ?? string.Empty),
                            ServerSetting.SmtpHost ?? string.Empty,
                            ServerSetting.SmtpPort.ToString(),
                            ServerSetting.SmtpUserName ?? string.Empty,
                            ServerSetting.SmtpPassword ?? string.Empty,
                            ServerSetting.SmtpSSL.ToString(),
                            ServerSetting.FromEmail ?? string.Empty,
                            ServerSetting.FromFullName ?? string.Empty);
                        if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // When mail sending error occurs..
                        {
                            Console.WriteLine(result); // Should log this with ActivityLog etc..
                            try
                            {
                                account.Updated = DateTime.UtcNow;
                                account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                                await _accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                                return View();
                            }
                            TempData["Error"] = _localizer["Error occurred while processing about sending account authentication mail"].Value;
                            return View();
                        }
                        #endregion
                        try
                        {
                            account.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                            account.Updated = DateTime.UtcNow;
                            await _accountRepository.UpdateAccountAsync(account);
                        }
                        catch
                        {
                            TempData["Error"] = _localizer["Error occurred while processing about mail send account status"].Value;
                            return View();
                        }
                        ViewBag.ResendEmail = true; // Enable email resend form
                        ViewBag.ResentEmailAddress = account.Email; // For displaying email address in email resend form
                        ViewBag.RepeatEmailSend = true; // Email resend confirmation
                        return View();
                    }
                    #endregion

                    #region Created and email verified
                    else // Created and email verified
                    {
                        try
                        {
                            account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
                            account.Updated = DateTime.UtcNow;
                            await _accountRepository.UpdateAccountAsync(account);
                        }
                        catch
                        {
                            TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                            return View();
                        }
                        TempData["Error"] = _localizer[EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated)].Value;
                        ViewBag.ResendEmail = false; // Enable login form
                        return View();
                    }
                    #endregion
                }
                #endregion
            }

            else // Invalid input
            {
                return RedirectToAction("Register", "Account");
            }
        }
    }
    #endregion

    #region ConfirmEmail
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string registrationToken)
    {
        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

        if (isAccountSessionExist) // If logged in
        {
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            if (loginedAccount.Role == Role.Admin) // If logged-in user role is Admin
            {
                return RedirectToAction("AdminIndex", "DashBoard");
            }
            else if (loginedAccount.Role == Role.User) // If logged-in user role is User
            {
                return RedirectToAction("UserIndex", "DashBoard");
            }
            else // If logged-in user role is neither Admin nor User, it's a DB Insert error
            {
                return null; // Wrong input produces wrong output
            }
        }
        else // If not logged in
        {
            Account account = null;

            #region 1. Compare inputted token[encrypted] with hashed tokens stored in all accounts to find the account.
            foreach (Account tempAccount in await _accountRepository.GetAllAsync())
            {
                try
                {
                    if (_rsa.Verify(tempAccount.RegistrationToken, registrationToken)) // RSA token verification
                    {
                        account = tempAccount;
                        break;
                    }
                }
                catch
                {

                }
            }
            #endregion

            #region 2. Could not find the account by Token.
            if (account == null)
            {
                ViewBag.InvalidToken = true;
                ViewBag.AccountCreated = false;
                return View();
            }
            #endregion

            #region 3. The account's Token has expired.
            if (!GUIDToken.IsTokenAlive(account.RegistrationToken))
            {
                ViewBag.InvalidToken = true;
                ViewBag.AccountCreated = false;
                return View();
            }
            #endregion

            #region 4. The account's EmailConfirmed is false. (Email not verified.)
            if (!account.EmailConfirmed)
            {
                account.RegistrationToken = null;
                account.EmailConfirmed = true;
                account.Updated = DateTime.UtcNow;
                account.Message = EnumHelper.GetDescription(AccountMessage.Success);
                try
                {
                    await _accountRepository.UpdateAccountAsync(account);
                }
                catch  // 2. Exception occurred during update
                {
                    ViewBag.InvalidToken = true;
                    ViewBag.AccountCreated = true;
                    TempData["Error"] = _localizer["Error occurred while processing about confirm email account status"].Value;
                    return View();
                }
                ViewBag.InvalidToken = false;
                ViewBag.AccountCreated = true;
                return View();
            }
            #endregion

            #region 5. The account's EmailConfirmed is true. (Email verified.)
            else
            {
                account.Updated = DateTime.UtcNow;
                account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
                try
                {
                    await _accountRepository.UpdateAccountAsync(account);
                }
                catch // 2. Exception occurred during update [should not happen].
                {
                    ViewBag.InvalidToken = true;
                    ViewBag.AccountCreated = true;
                    TempData["Error"] = _localizer["Error occurred while processing about account status"].Value;
                    return View();
                }
                ViewBag.InvalidToken = false;
                ViewBag.AccountCreated = false;
                return View();
            }
            #endregion
        }
    }
    #endregion

    #region ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

        if (isAccountSessionExist) // If logged in
        {
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            if (loginedAccount.Role == Role.Admin) // If logged-in user role is Admin
            {
                return RedirectToAction("AdminIndex", "DashBoard");
            }
            else if (loginedAccount.Role == Role.User) // If logged-in user role is User
            {
                return RedirectToAction("UserIndex", "DashBoard");
            }
            else // If logged-in user role is neither Admin nor User, it's a DB Insert error
            {
                return null; // Wrong input produces wrong output
            }
        }
        else // If not logged in
        {
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (ModelState.IsValid)
        {
            Account account = await _accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

            if (account == null) // No record created with inputted email.
            {
                ViewBag.mailSent = true;
                ViewBag.sentEmailAddress = loginInputViewModel.Email;
                return View();
            }

            if (!account.EmailConfirmed) // Email not verified
            {
                ViewBag.mailSent = true;
                ViewBag.sentEmailAddress = loginInputViewModel.Email;
                return View();
            }

            if (account.EmailConfirmed) // Email verified
            {
                string resetPasswordToken = GUIDToken.Generate();
                try
                {
                    #region Send Email
                    string result = await _mailClient.SendMailAsync(
                        account.Email,
                        _localizer["Maroik Reset Password"].Value,
                        _mailClient.GetMailResetPasswordBody(_rsa.Sign(resetPasswordToken), _localizer["Welcome to Maroik"].Value, _localizer["Click the link below to reset your password"].Value, _localizer["If this link does not work, please copy & paste this link to your Internet URL"].Value, ServerSetting.DomainName ?? string.Empty),
                        ServerSetting.SmtpHost ?? string.Empty,
                        ServerSetting.SmtpPort.ToString(),
                        ServerSetting.SmtpUserName ?? string.Empty,
                        ServerSetting.SmtpPassword ?? string.Empty,
                        ServerSetting.SmtpSSL.ToString(),
                        ServerSetting.FromEmail ?? string.Empty,
                        ServerSetting.FromFullName ?? string.Empty);
                    if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // When mail sending error occurs..
                    {
                        Console.WriteLine(result); // Should log this with ActivityLog etc..
                        account.Updated = DateTime.UtcNow;
                        account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                        await _accountRepository.UpdateAccountAsync(account);
                        ViewBag.mailSent = true;
                        ViewBag.sentEmailAddress = loginInputViewModel.Email;
                        return View();
                    }
                    #endregion

                    account.ResetPasswordToken = resetPasswordToken;
                    account.Updated = DateTime.UtcNow;
                    account.Message = EnumHelper.GetDescription(AccountMessage.ResetPasswordMail);
                    await _accountRepository.UpdateAccountAsync(account);
                    ViewBag.mailSent = true;
                    ViewBag.sentEmailAddress = loginInputViewModel.Email;
                    return View();
                }
                catch
                {
                    ViewBag.mailSent = true;
                    ViewBag.sentEmailAddress = loginInputViewModel.Email;
                    return View();
                }
            }
            ViewBag.mailSent = true;
            ViewBag.sentEmailAddress = loginInputViewModel.Email;
            return View();
        }
        else // Invalid input
        {
            return RedirectToAction("ForgotPassword", "Account");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string resetPasswordToken)
    {
        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

        if (isAccountSessionExist) // If logged in
        {
            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            if (loginedAccount.Role == Role.Admin) // If logged-in user role is Admin
            {
                return RedirectToAction("AdminIndex", "DashBoard");
            }
            else if (loginedAccount.Role == Role.User) // If logged-in user role is User
            {
                return RedirectToAction("UserIndex", "DashBoard");
            }
            else // If logged-in user role is neither Admin nor User, it's a DB Insert error
            {
                return null; // Wrong input produces wrong output
            }
        }
        else // If not logged in
        {
            Account account = null;

            #region 1. Compare inputted token[encrypted] with hashed tokens stored in all accounts to find the account.
            foreach (Account tempAccount in await _accountRepository.GetAllAsync())
            {
                try
                {
                    if (_rsa.Verify(tempAccount.ResetPasswordToken, resetPasswordToken)) // RSA token verification
                    {
                        account = tempAccount;
                        break;
                    }
                }
                catch
                {

                }
            }
            #endregion

            #region 2. Could not find the account by Token.
            if (account == null)
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 3. The account's Token has expired.
            if (!GUIDToken.IsTokenAlive(account.ResetPasswordToken))
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 4. The account's EmailConfirmed is false. (Email not verified.)
            if (!account.EmailConfirmed)
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 5. The account's EmailConfirmed is true. (Email verified.)
            else
            {
                ViewBag.FailToResetPassword = false;
                ViewBag.ResetPasswordToken = resetPasswordToken;
                return View();
            }
            #endregion
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> ResetPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Email));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (ModelState.IsValid)
        {
            Account account = null;

            #region 1. Compare inputted token[encrypted] with hashed tokens stored in all accounts to find the account.
            foreach (Account tempAccount in await _accountRepository.GetAllAsync())
            {
                try
                {
                    if (_rsa.Verify(tempAccount.ResetPasswordToken, loginInputViewModel.ResetPasswordToken)) // RSA token verification
                    {
                        account = tempAccount;
                        break;
                    }
                }
                catch
                {

                }
            }
            #endregion

            #region 2. Could not find the account by Token.
            if (account == null)
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 3. The account's Token has expired.
            if (!GUIDToken.IsTokenAlive(account.ResetPasswordToken))
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 4. The account's EmailConfirmed is false. (Email not verified.)
            if (!account.EmailConfirmed)
            {
                ViewBag.FailToResetPassword = true;
                return View();
            }
            #endregion

            #region 5. The account's EmailConfirmed is true. (Email verified.)
            else
            {
                account.ResetPasswordToken = null;
                account.HashedPassword = _passwordService.HashPassword(loginInputViewModel.Password);
                account.Locked = false; // Unlock
                account.LoginAttempt = 0; // Reset login attempt count
                account.Updated = DateTime.UtcNow;
                account.Message = EnumHelper.GetDescription(AccountMessage.SuccessToResetPassword);
                try
                {
                    await _accountRepository.UpdateAccountAsync(account);
                }
                catch  // 2. Exception occurred during update
                {
                    ViewBag.FailToResetPassword = true;
                    TempData["Error"] = _localizer["Error occurred while processing about reset password"].Value;
                    return View();
                }
                ViewBag.resetPasswordComplete = true;
                return View();
            }
            #endregion
        }
        else // Invalid input
        {
            return RedirectToAction("ResetPassword", "Account");
        }
    }
    #endregion
}