using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Models.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers;

public class AccountController(
    IHtmlLocalizer<AccountController> localizer,
    IAccountRepository accountRepository,
    IMailRepository mailRepository)
    : Controller
{
    private readonly Rsa _rsa = new(RsaType.Rsa2, Encoding.UTF8, Rsa.PrivateKey, Rsa.PublicKey);

    #region ConsentForm

    [HttpGet]
    public IActionResult ConsentForm()
    {
        var isAccountSessionExist =
            HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (!isAccountSessionExist) return View();
        var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte));
        return loginedAccount.Role switch
        {
            // 로그인한 사용자 권한이 Admin일 경우
            Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
            // 로그인한 사용자 권한이 User일 경우
            Role.User => RedirectToAction("UserIndex", "DashBoard"),
            _ => null
        };

        // 로그인 안 된 상태라면
    }

    #endregion

    #region Login

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Contains("MSIE") ||
            userAgent.Contains("Trident")) // IsInternetExplorer (차단 이유: Nonfactor Data Grid에서 문제 생김...)
        {
            TempData["Error"] = localizer["BlockInternetExplorer"].Value;
            return View();
        }

        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (!ModelState.IsValid) return View();
        var tempAccount = await accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

        if (tempAccount == null) // no account exists
        {
            TempData["Error"] = localizer["Email or Password is wrong"].Value;
            return View();
        }

        if (tempAccount.Deleted)
        {
            TempData["Error"] =
                localizer["Your Account is Deleted, Please create your new account by clicking Sign up Button"]
                    .Value;
            return View();
        }

        if (tempAccount.Locked)
        {
            TempData["Error"] =
                localizer["Your Account is Locked, Please reset your password by clicking Forgot password Button"]
                    .Value;
            return View();
        }

        if (!_rsa.Verify(loginInputViewModel.Password ?? "", tempAccount.HashedPassword)) // wrong password
        {
            tempAccount.LoginAttempt++;

            if (tempAccount.LoginAttempt == ServerSetting.MaxLoginAttempt)
            {
                tempAccount.Locked = true;
                tempAccount.Message = EnumHelper.GetDescription(AccountMessage.AccountLocked);
                await accountRepository.UpdateAccountAsync(tempAccount);
                TempData["Error"] =
                    localizer[
                            "Your Account is Locked, Please reset your password by clicking Forgot password Button"]
                        .Value;
            }
            else
            {
                await accountRepository.UpdateAccountAsync(tempAccount);
                TempData["Error"] = localizer["Email or Password is wrong"].Value;
            }

            return View();
        }

        switch (tempAccount.EmailConfirmed)
        {
            case true when tempAccount.AgreedServiceTerms:
                tempAccount.LoginAttempt = 0;
                await accountRepository.UpdateAccountAsync(tempAccount);
                tempAccount.HashedPassword = null;
                HttpContext.Session.Set(EnumHelper.GetDescription(Session.Account),
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));

                #region DB Cache

                return tempAccount.Role switch
                {
                    Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
                    Role.User => RedirectToAction("UserIndex", "DashBoard"),
                    _ => (IActionResult)null
                };

            #endregion

            case false:
                TempData["Error"] = localizer["Email verification was not completed. Please try sign up again."]
                    .Value;
                break;
            default:
            {
                if (tempAccount.AgreedServiceTerms == false)
                {
                    TempData["Error"] =
                        localizer["Agreed Service Terms was not checked. Please try sign up again and login again."]
                            .Value;
                }
                else
                {
                    TempData["Error"] =
                        localizer[
                                "Email verification was not completed and Agreed Service Terms was not checked. Please try sign up again."]
                            .Value;
                }

                break;
            }
        }

        return View();
    }

    #endregion

    #region Logout

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(EnumHelper.GetDescription(Session.Account));
        return RedirectToAction("AnonymousIndex", "DashBoard");
    }

    #endregion

    #region Register

    [HttpGet]
    public IActionResult Register()
    {
        var isAccountSessionExist =
            HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (!isAccountSessionExist) return View();
        var loginedAccount =
            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
        return loginedAccount.Role switch
        {
            // 로그인한 사용자 권한이 Admin일 경우
            Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
            // 로그인한 사용자 권한이 User일 경우
            Role.User => RedirectToAction("UserIndex", "DashBoard"),
            _ => null
        };

        // 로그인 안 된 상태라면
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel)
    {
        if (!string.IsNullOrEmpty(loginInputViewModel.Password)) // 회원 가입
        {
            if (!ModelState.IsValid) return RedirectToAction("Register", "Account"); // 입력 값 검증
            if (!loginInputViewModel.AgreedServiceTerms) // 계약서 동의 하지 않을 시,
            {
                TempData["Error"] = localizer["Please check the consent form"].Value;
                return View();
            }

            if (string.IsNullOrEmpty(loginInputViewModel.TimeZoneIanaId)) // TimeZone 선택 하지 않을 시,
            {
                TempData["Error"] = localizer["Please select time zone"].Value;
                return View();
            }

            var account = await accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

            #region 새롭게 회원가입을 진행하는 경우

            if (account == null) // 입력된 ID로 생성된 Record가 없다.
            {
                var registrationToken = GuidToken.Generate();
                loginInputViewModel.RegistrationToken = registrationToken;
                try
                {
                    await accountRepository.CreateAccountAsync
                    (
                        new Account
                        {
                            Email = loginInputViewModel.Email,
                            HashedPassword = _rsa.Sign(loginInputViewModel.Password), // 암호화
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
                    if (((Npgsql.NpgsqlException)e.InnerException)?.SqlState ==
                        Npgsql.PostgresErrorCodes.UniqueViolation)
                    {
                        Console.WriteLine(localizer["Input is invalid"].Value); // ActivityLog 등으로 로그 남겨야 한다..
                        TempData["Error"] =
                            $"'{loginInputViewModel.Nickname}' {localizer["is a Nickname that already exists."].Value} {localizer["Please enter another Nickname."].Value}";
                    }
                    else
                    {
                        Console.WriteLine(localizer["Input is invalid"].Value); // ActivityLog 등으로 로그 남겨야 한다..
                        TempData["Error"] = localizer["Error occurred while processing about account registration"]
                            .Value;
                    }

                    return View();
                }
                catch
                {
                    Console.WriteLine(localizer["Input is invalid"].Value); // ActivityLog 등으로 로그 남겨야 한다..
                    TempData["Error"] = localizer["Error occurred while processing about account registration"]
                        .Value;
                    return View();
                }

                #region 메일 전송

                Mail mail = new()
                {
                    Subject = localizer["Maroik Email Confirmation"].Value
                };
                loginInputViewModel.RegistrationToken = _rsa.Sign(loginInputViewModel.RegistrationToken); // 암호화
                mail.Body = mailRepository.GetMailConfirmationBody(loginInputViewModel,
                    localizer["Welcome to Maroik"].Value,
                    localizer["Click the link below to verify your Email"].Value,
                    localizer["If this link does not work, please copy & paste this link to your Internet URL"]
                        .Value);
                mail.ToMailIds =
                [
                    loginInputViewModel.Email
                ];
                var result = await mailRepository.SendMailAsync(mail);
                if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // 메일 전송 오류 발생 했을 시..
                {
                    Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                    loginInputViewModel.RegistrationToken = registrationToken;
                    try
                    {
                        await accountRepository.UpdateAccountAsync
                        (
                            new Account
                            {
                                Email = loginInputViewModel.Email,
                                HashedPassword = _rsa.Sign(loginInputViewModel.Password), // 암호화
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
                        TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                        return View();
                    }

                    TempData["Error"] =
                        localizer["Error occurred while processing about sending account authentication mail"]
                            .Value;
                    return View();
                }

                #endregion

                ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                ViewBag.ResentEmailAddress = loginInputViewModel.Email; // 이메일 재전송 폼 이메일 주소 표시용
                return View();
            }

            #endregion

            #region 회원가입이 된 경우

            // 입력된 ID로 생성된 Record 있다.

            #region 이메일 인증이 안 된 경우

            if (!account.EmailConfirmed) // 생성은 되었으나 Email 확인 안 함
            {
                #region 메일 전송

                Mail mail = new()
                {
                    Subject = localizer["Maroik Email Confirmation"].Value
                };
                if (string.IsNullOrEmpty(account.RegistrationToken))
                {
                    account.RegistrationToken = GuidToken.Generate();
                }

                mail.Body = mailRepository.GetMailConfirmationBody(
                    new LoginInputViewModel() { RegistrationToken = _rsa.Sign(account.RegistrationToken) },
                    localizer["Welcome to Maroik"].Value,
                    localizer["Click the link below to verify your Email"].Value,
                    localizer["If this link does not work, please copy & paste this link to your Internet URL"]
                        .Value);
                mail.ToMailIds =
                [
                    account.Email
                ];
                var result = await mailRepository.SendMailAsync(mail);
                if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // 메일 전송 오류 발생 했을 시..
                {
                    Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                    try
                    {
                        account.Updated = DateTime.UtcNow;
                        account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                        await accountRepository.UpdateAccountAsync(account);
                    }
                    catch
                    {
                        TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                        return View();
                    }

                    TempData["Error"] =
                        localizer["Error occurred while processing about sending account authentication mail"]
                            .Value;
                    return View();
                }

                #endregion

                try
                {
                    account.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                    account.Updated = DateTime.UtcNow;
                    await accountRepository.UpdateAccountAsync(account);
                }
                catch
                {
                    TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                    return View();
                }

                ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                ViewBag.ResentEmailAddress = account.Email; // 이메일 재전송 폼 이메일 주소 표시용
                return View();
            }

            #endregion

            #region 이메일 인증이 된 경우

            // 계정 생성 및 이메일 확인 완료
            try
            {
                account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
                account.Updated = DateTime.UtcNow;
                account.AgreedServiceTerms = true;
                await accountRepository.UpdateAccountAsync(account);
            }
            catch
            {
                TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                return View();
            }

            TempData["Error"] = localizer[EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated)].Value;
            ViewBag.ResendEmail = false; // 로그인 폼 활성화
            return View();

            #endregion

            #endregion

            // 입력 값 잘못 됨
        }

        // 이메일 재전송
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));

        if (!ModelState.IsValid) return RedirectToAction("Register", "Account"); // 입력 값 검증

        {
            var account = await accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

            #region 이메일 재전송 하는데 받은 Email로 DB 조회를 했는데 Record가 없다. (버그임)

            if (account == null) // 입력된 ID로 생성된 Record 없다.
            {
                TempData["Error"] = localizer["Failed to resend email"].Value;
                ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                return View();
            }

            #endregion

            #region 이메일 재전송

            // 입력된 ID로 생성된 Record 있다.

            #region 생성은 되었으나 Email 확인 안 함

            if (!account.EmailConfirmed) // 생성은 되었으나 Email 확인 안 함.
            {
                #region 메일 전송

                Mail mail = new()
                {
                    Subject = localizer["Maroik Email Confirmation"].Value,
                    Body = mailRepository.GetMailConfirmationBody(
                        new LoginInputViewModel { RegistrationToken = _rsa.Sign(account.RegistrationToken) },
                        localizer["Welcome to Maroik"].Value,
                        localizer["Click the link below to verify your Email"].Value,
                        localizer["If this link does not work, please copy & paste this link to your Internet URL"]
                            .Value),
                    ToMailIds =
                    [
                        account.Email
                    ]
                };
                var result = await mailRepository.SendMailAsync(mail);
                if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // 메일 전송 오류 발생 했을 시..
                {
                    Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                    try
                    {
                        account.Updated = DateTime.UtcNow;
                        account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                        await accountRepository.UpdateAccountAsync(account);
                    }
                    catch
                    {
                        TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                        return View();
                    }

                    TempData["Error"] =
                        localizer["Error occurred while processing about sending account authentication mail"]
                            .Value;
                    return View();
                }

                #endregion

                try
                {
                    account.Message = EnumHelper.GetDescription(AccountMessage.VerifyEmail);
                    account.Updated = DateTime.UtcNow;
                    await accountRepository.UpdateAccountAsync(account);
                }
                catch
                {
                    TempData["Error"] = localizer["Error occurred while processing about mail send account status"]
                        .Value;
                    return View();
                }

                ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                ViewBag.ResentEmailAddress = account.Email; // 이메일 재전송 폼 이메일 주소 표시용
                ViewBag.RepeatEmailSend = true; // 이메일 재발송 확인
                return View();
            }

            #endregion

            #region 생성되고 Email 확인함

            // 생성되고 Email 확인함
            try
            {
                account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
                account.Updated = DateTime.UtcNow;
                await accountRepository.UpdateAccountAsync(account);
            }
            catch
            {
                TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
                return View();
            }

            TempData["Error"] = localizer[EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated)].Value;
            ViewBag.ResendEmail = false; // 로그인 폼 활성화
            return View();

            #endregion

            #endregion
        }

        // 입력 값 잘못 됨
    }

    #endregion

    #region ConfirmEmail

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string registrationToken)
    {
        var isAccountSessionExist =
            HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (isAccountSessionExist) // 로그인 된 상태라면
        {
            var loginedAccount =
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            return loginedAccount.Role switch
            {
                // 로그인한 사용자 권한이 Admin일 경우
                Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
                // 로그인한 사용자 권한이 User일 경우
                Role.User => RedirectToAction("UserIndex", "DashBoard"),
                _ => null
            };
        }

        // 로그인 안 된 상태라면
        Account account = null;

        #region 1.	입력된 token[암호화 됨]이랑 모든 계정에 저장된 토큰 해쉬화 후 비교해서 해당 계정 찾아낸다.

        foreach (var tempAccount in await accountRepository.GetAllAsync())
        {
            try
            {
                if (!_rsa.Verify(tempAccount.RegistrationToken, registrationToken)) continue;
                account = tempAccount;
                break;
            }
            catch
            {
                // ignored
            }
        }

        #endregion

        #region 2.	해당 계정을 Token으로 찾지 못했다. 또는 해당 계정의 Token 만료 되었다.

        if (account == null || !GuidToken.IsTokenAlive(account.RegistrationToken))
        {
            ViewBag.InvalidToken = true;
            ViewBag.AccountCreated = false;
            return View();
        }

        #endregion

        #region 3.	해당 계정의 EmailConfirmd가 false이다. (이메일 인증을 하지 않았다.)

        if (!account.EmailConfirmed)
        {
            account.RegistrationToken = null;
            account.EmailConfirmed = true;
            account.Updated = DateTime.UtcNow;
            account.Message = EnumHelper.GetDescription(AccountMessage.Success);
            try
            {
                await accountRepository.UpdateAccountAsync(account);
            }
            catch // 2. Update 시, 예외 발생 했다
            {
                ViewBag.InvalidToken = true;
                ViewBag.AccountCreated = true;
                TempData["Error"] = localizer["Error occurred while processing about confirm email account status"]
                    .Value;
                return View();
            }

            ViewBag.InvalidToken = false;
            ViewBag.AccountCreated = true;
            return View();
        }

        #endregion

        #region 4.	해당 계정의 EmailConfirmd가 true이다. (이메일 인증을 하였다.)

        account.Updated = DateTime.UtcNow;
        account.Message = EnumHelper.GetDescription(AccountMessage.UserAlreadyCreated);
        try
        {
            await accountRepository.UpdateAccountAsync(account);
        }
        catch // 2.	Update 시, 예외 발생 했다 [있을 수 없는 일].
        {
            ViewBag.InvalidToken = true;
            ViewBag.AccountCreated = true;
            TempData["Error"] = localizer["Error occurred while processing about account status"].Value;
            return View();
        }

        ViewBag.InvalidToken = false;
        ViewBag.AccountCreated = false;
        return View();

        #endregion
    }

    #endregion

    #region ForgotPassword

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        var isAccountSessionExist =
            HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (!isAccountSessionExist) return View();
        var loginedAccount =
            JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
        return loginedAccount.Role switch
        {
            // 로그인한 사용자 권한이 Admin일 경우
            Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
            // 로그인한 사용자 권한이 User일 경우
            Role.User => RedirectToAction("UserIndex", "DashBoard"),
            _ => null
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken] //To Prevent CSRF attack
    public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ForgotPassword", "Account");
        var account = await accountRepository.GetAccountByEmailAsync(loginInputViewModel.Email);

        if (account == null) // 입력 된 Email로 생성된 Record 없다.
        {
            ViewBag.mailSent = true;
            ViewBag.sentEmailAddress = loginInputViewModel.Email;
            return View();
        }

        switch (account.EmailConfirmed)
        {
            // 이메일 인증 안 됨
            case false:
                break;
            // 이메일 인증 됨
            case true:
            {
                var resetPasswordToken = GuidToken.Generate();
                try
                {
                    #region 메일 전송

                    Mail mail = new()
                    {
                        Subject = localizer["Maroik Reset Password"].Value,
                        Body = mailRepository.GetMailResetPasswordBody(
                            new LoginInputViewModel { ResetPasswordToken = _rsa.Sign(resetPasswordToken) },
                            localizer["Welcome to Maroik"].Value,
                            localizer["Click the link below to reset your password"].Value,
                            localizer[
                                    "If this link does not work, please copy & paste this link to your Internet URL"]
                                .Value),
                        ToMailIds =
                        [
                            account.Email
                        ]
                    };
                    var result = await mailRepository.SendMailAsync(mail);
                    if (result != EnumHelper.GetDescription(AccountMessage.MailSent)) // 메일 전송 오류 발생 했을 시..
                    {
                        Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                        account.Updated = DateTime.UtcNow;
                        account.Message = EnumHelper.GetDescription(AccountMessage.FailToMailSent);
                        await accountRepository.UpdateAccountAsync(account);
                        ViewBag.mailSent = true;
                        ViewBag.sentEmailAddress = loginInputViewModel.Email;
                        return View();
                    }

                    #endregion

                    account.ResetPasswordToken = resetPasswordToken;
                    account.Updated = DateTime.UtcNow;
                    account.Message = EnumHelper.GetDescription(AccountMessage.ResetPasswordMail);
                    await accountRepository.UpdateAccountAsync(account);
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
        }

        ViewBag.mailSent = true;
        ViewBag.sentEmailAddress = loginInputViewModel.Email;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string resetPasswordToken)
    {
        var isAccountSessionExist =
            HttpContext.Session.TryGetValue(EnumHelper.GetDescription(Session.Account), out var resultByte);

        if (isAccountSessionExist) // 로그인 된 상태라면
        {
            var loginedAccount =
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
            return loginedAccount.Role switch
            {
                // 로그인한 사용자 권한이 Admin일 경우
                Role.Admin => RedirectToAction("AdminIndex", "DashBoard"),
                // 로그인한 사용자 권한이 User일 경우
                Role.User => RedirectToAction("UserIndex", "DashBoard"),
                _ => null
            };
        }

        // 로그인 안 된 상태라면
        Account account = null;

        #region 1.	입력된 token[암호화 됨]이랑 모든 계정에 저장된 토큰 해쉬화 후 비교해서 해당 계정 찾아낸다.

        foreach (var tempAccount in await accountRepository.GetAllAsync())
        {
            try
            {
                if (!_rsa.Verify(tempAccount.ResetPasswordToken, resetPasswordToken)) continue;
                account = tempAccount;
                break;
            }
            catch
            {
                // ignored
            }
        }

        #endregion

        #region 2.	해당 계정을 Token으로 찾지 못했다. 또는 해당 계정의 Token 만료 되었다. 또는 해당 계정의 EmailConfirmd가 false이다. (이메일 인증을 하지 않았다.)

        if (account == null || !GuidToken.IsTokenAlive(account.ResetPasswordToken) || !account.EmailConfirmed)
        {
            ViewBag.FailToResetPassword = true;
            return View();
        }

        #endregion

        #region 3.	해당 계정의 EmailConfirmd가 true이다. (이메일 인증을 하였다.)

        ViewBag.FailToResetPassword = false;
        ViewBag.ResetPasswordToken = resetPasswordToken;
        return View();

        #endregion
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(LoginInputViewModel loginInputViewModel)
    {
        _ = ModelState.Remove(nameof(loginInputViewModel.Email));
        _ = ModelState.Remove(nameof(loginInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(loginInputViewModel.Password));

        if (!ModelState.IsValid) return RedirectToAction("ResetPassword", "Account");
        Account account = null;

        #region 1.	입력된 token[암호화 됨]이랑 모든 계정에 저장된 토큰 해쉬화 후 비교해서 해당 계정 찾아낸다.

        foreach (var tempAccount in await accountRepository.GetAllAsync())
        {
            try
            {
                if (!_rsa.Verify(tempAccount.ResetPasswordToken, loginInputViewModel.ResetPasswordToken))
                    continue;
                account = tempAccount;
                break;
            }
            catch
            {
                // ignored
            }
        }

        #endregion

        #region 2.	해당 계정을 Token으로 찾지 못했다. 또는 해당 계정의 Token 만료 되었다. 또는 해당 계정의 EmailConfirmd가 false이다. (이메일 인증을 하지 않았다.)

        if (account == null || !GuidToken.IsTokenAlive(account.ResetPasswordToken) || !account.EmailConfirmed)
        {
            ViewBag.FailToResetPassword = true;
            return View();
        }

        #endregion

        #region 3.	해당 계정의 EmailConfirmd가 true이다. (이메일 인증을 하였다.)

        account.ResetPasswordToken = null;
        account.HashedPassword = _rsa.Sign(loginInputViewModel.Password); // 암호화
        account.Locked = false; // 잠금 해제
        account.LoginAttempt = 0; // 로그인 시도 횟수 초기화
        account.Updated = DateTime.UtcNow;
        account.Message = EnumHelper.GetDescription(AccountMessage.SuccessToResetPassword);
        try
        {
            await accountRepository.UpdateAccountAsync(account);
        }
        catch // 2. Update 시, 예외 발생 했다
        {
            ViewBag.FailToResetPassword = true;
            TempData["Error"] = localizer["Error occurred while processing about reset password"].Value;
            return View();
        }

        ViewBag.resetPasswordComplete = true;
        return View();

        #endregion
    }

    #endregion
}