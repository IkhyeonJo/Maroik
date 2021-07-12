using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.Site.Models.ViewModels.Account;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyLaboratory.Site.Common;
using MyLaboratory.Site.Services;
using Newtonsoft.Json;
using System.Text;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Contracts;
using MyLaboratory.Site.Helpers;

namespace MyLaboratory.Site.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        private readonly IMailRepository mailRepository;

        public AccountController(ILogger<AccountController> logger, IAccountRepository accountRepository, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IMailRepository mailRepository)
        {
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
            this.mailRepository = mailRepository;
        }

        #region 동의서
        [HttpGet]
        public IActionResult ConsentForm()
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                return View();
            }
        }
        #endregion

        #region 로그인
        [HttpGet]
        public IActionResult Login()
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //To Prevent CSRF attack
        public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
        {
            string userAgent = Request.Headers["User-Agent"].ToString();
            if (userAgent.Contains("MSIE") || userAgent.Contains("Trident")) // IsInternetExplorer (차단 이유: DataTable에서 문제생김...)
            {
                TempData["Error"] = "이 사이트는 Internet Explorer를 지원하지 않습니다. 다른 브라우저를 이용하시길 바랍니다.";
                return RedirectToAction("Login", "Account");
            }

            ModelState.Remove(nameof(loginInputViewModel.FullName));

            if (ModelState.IsValid)
            {
                var tempAccount = await accountRepository.GetAccountAsync(loginInputViewModel.Email);

                if (tempAccount == null) // no account exist
                {
                    TempData["Error"] = "Username or Password is invalid";
                    return RedirectToAction("Login", "Account");
                }

                if (!Crypto.VerifyHashedPassword(tempAccount.HashedPassword, loginInputViewModel.Password)) // wrong password
                {
                    TempData["Error"] = "Username or Password is invalid";
                    return RedirectToAction("Login", "Account");
                }

                tempAccount.HashedPassword = null;

                if (tempAccount.EmailConfirmed == true && tempAccount.AgreedServiceTerms == true)
                {
                    HttpContext.Session.Set("AccountSession", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));

                    #region DB Cache
                    if (tempAccount.Role == Role.Admin) // 로그인 권한이 Admin인 경우
                    {
                        DbCache.AdminCategories = categoryRepository.AdminCategories;
                        DbCache.AdminSubCategories = subCategoryRepository.AdminSubCategories;
                        return RedirectToAction("Index", "DashBoard");
                    }
                    else if (tempAccount.Role == Role.User) // 로그인 권한이 User인 경우
                    {
                        DbCache.UserCategories = categoryRepository.UserCategories;
                        DbCache.UserSubCategories = subCategoryRepository.UserSubCategories;
                        return RedirectToAction("Index", "UserDashBoard");
                    }
                    else // 로그인 권한은 Admin 또는 User만 있음 이 경우는 버그 발생한 것임
                    {
                        return null;
                    }
                }
                else
                {
                    TempData["Error"] = "Email verification was not completed. Please try sign up again.";
                    return RedirectToAction("Login", "Account");
                }
                #endregion
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }
        #endregion

        #region 로그아웃
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AccountSession");
            return RedirectToAction("Login", "Account");
        }
        #endregion

        #region 계정 생성
        [HttpGet]
        public IActionResult Register()
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //To Prevent CSRF attack
        public async Task<IActionResult> Register(LoginInputViewModel loginInputViewModel)
        {
            if (!string.IsNullOrEmpty(loginInputViewModel.Password)) // 회원 가입
            {
                if (ModelState.IsValid) // 입력 값 검증
                {
                    if (!loginInputViewModel.AgreedServiceTerms) // 계약서 동의 하지 않을 시,
                    {
                        TempData["Error"] = "Please check the consent form";
                        return View();
                    }

                    var account = await accountRepository.GetAccountAsync(loginInputViewModel.Email);

                    #region 새롭게 회원가입을 진행하는 경우
                    if (account == null) // 입력된 ID로 생성된 Record가 없다.
                    {
                        string registrationToken = GUIDToken.Generate();
                        loginInputViewModel.RegistrationToken = registrationToken;
                        try
                        {
                            await accountRepository.CreateAccountAsync
                                (
                                new Account()
                                {
                                    Email = loginInputViewModel.Email,
                                    HashedPassword = Crypto.HashPassword(loginInputViewModel.Password),
                                    FullName = loginInputViewModel.FullName,
                                    Role = Role.User,
                                    Locked = false,
                                    EmailConfirmed = false,
                                    AgreedServiceTerms = true,
                                    RegistrationToken = loginInputViewModel.RegistrationToken,
                                    ResetPasswordToken = null,
                                    Created = DateTime.UtcNow,
                                    Updated = DateTime.UtcNow,
                                    Message = AccountMessage.UserCreatedVerifyEmail
                                }
                                );
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString()); // ActivityLog 등으로 로그 남겨야 한다..
                            TempData["Error"] = "Error occurred when processing about account registration";
                            return View();
                        }
                        #region 메일 전송
                        Mail mail = new Mail();
                        mail.Subject = "MyLaboratory Email Confirmation";
                        loginInputViewModel.RegistrationToken = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(loginInputViewModel.RegistrationToken); // 암호화
                        mail.Body = mailRepository.GetMailConfirmationBody(loginInputViewModel);
                        mail.ToMailIds = new List<string>
                        {
                            loginInputViewModel.Email
                        };
                        string result = await mailRepository.SendMail(mail);
                        if (result != AccountMessage.MailSent) // 메일 전송 오류 발생 했을 시..
                        {
                            Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                            loginInputViewModel.RegistrationToken = registrationToken;
                            try
                            {
                                await accountRepository.UpdateAccountAsync
                                    (
                                    new Account()
                                    {
                                        Email = loginInputViewModel.Email,
                                        HashedPassword = Crypto.HashPassword(loginInputViewModel.Password),
                                        FullName = loginInputViewModel.FullName,
                                        Role = Role.User,
                                        Locked = false,
                                        EmailConfirmed = false,
                                        AgreedServiceTerms = true,
                                        RegistrationToken = loginInputViewModel.RegistrationToken,
                                        ResetPasswordToken = null,
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Message = AccountMessage.FailToMailSent
                                    });
                            }
                            catch
                            {
                                TempData["Error"] = "Error occurred when processing about mail fail account status";
                                return View();
                            }
                            TempData["Error"] = "Error occurred when processing about sending account authentication mail";
                            return View();
                        }
                        #endregion
                        ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                        ViewBag.ResentEmailAddress = loginInputViewModel.Email; // 이메일 재전송 폼 이메일 주소 표시용
                        return View();
                    }
                    #endregion

                    #region 회원가입이 된 경우
                    else // 입력된 ID로 생성된 Record 있다.
                    {
                        #region 이메일 인증이 안 된 경우
                        if (!account.EmailConfirmed) // 생성은 되었으나 Email 확인 안 함
                        {
                            #region 메일 전송
                            Mail mail = new Mail();
                            mail.Subject = "MyLaboratory Email Confirmation";
                            mail.Body = mailRepository.GetMailConfirmationBody(new LoginInputViewModel() { RegistrationToken = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(account.RegistrationToken) });
                            mail.ToMailIds = new List<string>
                            {
                                account.Email
                            };
                            string result = await mailRepository.SendMail(mail);
                            if (result != AccountMessage.MailSent) // 메일 전송 오류 발생 했을 시..
                            {
                                Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                                try
                                {
                                    account.Updated = DateTime.UtcNow;
                                    account.Message = AccountMessage.FailToMailSent;
                                    await accountRepository.UpdateAccountAsync(account);
                                }
                                catch
                                {
                                    TempData["Error"] = "Error occurred when processing about mail fail account status";
                                    return View();
                                }
                                TempData["Error"] = "Error occurred when processing about sending account authentication mail";
                                return View();
                            }
                            #endregion
                            try
                            {
                                account.Message = AccountMessage.VerifyEmail;
                                account.Updated = DateTime.UtcNow;
                                await accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = "Error occurred when processing about mail send account status";
                                return View();
                            }
                            ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                            ViewBag.ResentEmailAddress = account.Email; // 이메일 재전송 폼 이메일 주소 표시용
                            return View();
                        }
                        #endregion

                        #region 이메일 인증이 된 경우
                        else // 계정 생성 및 이메일 확인 완료
                        {
                            try
                            {
                                account.Message = AccountMessage.UserAlreadyCreated;
                                account.Updated = DateTime.UtcNow;
                                await accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = "Error occurred when processing about mail send account status";
                                return View();
                            }
                            TempData["Error"] = AccountMessage.UserAlreadyCreated;
                            ViewBag.ResendEmail = false; // 로그인 폼 활성화
                            return View();
                        }
                        #endregion
                    }
                    #endregion
                }
                else // 입력 값 잘못 됨
                {
                    return RedirectToAction("Register", "Account");
                }
            }
            else // 이메일 재전송
            {
                ModelState.Remove(nameof(loginInputViewModel.Password));
                ModelState.Remove(nameof(loginInputViewModel.FullName));

                if (ModelState.IsValid) // 입력 값 검증
                {
                    var account = await accountRepository.GetAccountAsync(loginInputViewModel.Email);

                    #region 이메일 재전송 하는데 받은 Email로 DB 조회를 했는데 Record가 없다. (버그임)
                    if (account == null) // 입력된 ID로 생성된 Record 없다.
                    {
                        TempData["Error"] = AccountMessage.FailToResendEmail;
                        ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                        return View();
                    }
                    #endregion

                    #region 이메일 재전송
                    else // 입력된 ID로 생성된 Record 있다.
                    {
                        #region 생성은 되었으나 Email 확인 안 함
                        if (!account.EmailConfirmed) // 생성은 되었으나 Email 확인 안 함.
                        {
                            #region 메일 전송
                            Mail mail = new Mail();
                            mail.Subject = "MyLaboratory Email Confirmation";
                            mail.Body = mailRepository.GetMailConfirmationBody(new LoginInputViewModel() { RegistrationToken = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(account.RegistrationToken) });
                            mail.ToMailIds = new List<string>
                            {
                                account.Email
                            };
                            string result = await mailRepository.SendMail(mail);
                            if (result != AccountMessage.MailSent) // 메일 전송 오류 발생 했을 시..
                            {
                                Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                                try
                                {
                                    account.Updated = DateTime.UtcNow;
                                    account.Message = AccountMessage.FailToMailSent;
                                    await accountRepository.UpdateAccountAsync(account);
                                }
                                catch
                                {
                                    TempData["Error"] = "Error occurred when processing about mail fail account status";
                                    return View();
                                }
                                TempData["Error"] = "Error occurred when processing about sending account authentication mail";
                                return View();
                            }
                            #endregion
                            try
                            {
                                account.Message = AccountMessage.VerifyEmail;
                                account.Updated = DateTime.UtcNow;
                                await accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = "Error occurred when processing about mail send account status";
                                return View();
                            }
                            ViewBag.ResendEmail = true; // 이메일 재전송 폼 활성화
                            ViewBag.ResentEmailAddress = account.Email; // 이메일 재전송 폼 이메일 주소 표시용
                            ViewBag.RepeatEmailSend = true; // 이메일 재발송 확인
                            return View();
                        }
                        #endregion

                        #region 생성되고 Email 확인함
                        else // 생성되고 Email 확인함
                        {
                            try
                            {
                                account.Message = AccountMessage.UserAlreadyCreated;
                                account.Updated = DateTime.UtcNow;
                                await accountRepository.UpdateAccountAsync(account);
                            }
                            catch
                            {
                                TempData["Error"] = "Error occurred when processing about account status";
                                return View();
                            }
                            TempData["Error"] = AccountMessage.UserAlreadyCreated;
                            ViewBag.ResendEmail = false; // 로그인 폼 활성화
                            return View();
                        }
                        #endregion
                    }
                    #endregion
                }

                else // 입력 값 잘못 됨
                {
                    return RedirectToAction("Register", "Account");
                }
            }
        }
        #endregion

        #region 이메일 확인
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string registrationToken)
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                Account account = null;

                #region 1.	입력된 token[암호화 됨]이랑 모든 계정에 저장된 토큰 해쉬화 후 비교해서 해당 계정 찾아낸다.
                var rsaHelper = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey);

                foreach (var tempAccount in await accountRepository.GetAllAsync())
                {
                    try
                    {
                        if (rsaHelper.Verify(tempAccount.RegistrationToken, registrationToken)) // 암호화된 토큰과 DB 사용자 토큰이 같다
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

                #region 2.	해당 계정을 Token으로 찾지 못했다.
                if (account == null)
                {
                    ViewBag.InvalidToken = true;
                    ViewBag.AccountCreated = false;
                    return View();
                }
                #endregion

                #region 3.	해당 계정의 Token 만료 되었다.
                if (!GUIDToken.IsTokenAlive(account.RegistrationToken))
                {
                    ViewBag.InvalidToken = true;
                    ViewBag.AccountCreated = false;
                    return View();
                }
                #endregion

                #region 4.	해당 계정의 EmailConfirmd가 false이다. (이메일 인증을 하지 않았다.)
                if (!account.EmailConfirmed)
                {
                    account.RegistrationToken = null;
                    account.EmailConfirmed = true;
                    account.Updated = DateTime.UtcNow;
                    account.Message = AccountMessage.Success;
                    try
                    {
                        await accountRepository.UpdateAccountAsync(account);
                    }
                    catch  // 2. Update 시, 예외 발생 했다
                    {
                        ViewBag.InvalidToken = true;
                        ViewBag.AccountCreated = true;
                        TempData["Error"] = "Error occurred when processing about confirm email account status";
                        return View();
                    }
                    ViewBag.InvalidToken = false;
                    ViewBag.AccountCreated = true;
                    return View();
                }
                #endregion

                #region 5.	해당 계정의 EmailConfirmd가 true이다. (이메일 인증을 하였다.)
                else
                {
                    account.Updated = DateTime.UtcNow;
                    account.Message = AccountMessage.UserAlreadyCreated;
                    try
                    {
                        await accountRepository.UpdateAccountAsync(account);
                    }
                    catch // 2.	Update 시, 예외 발생 했다 [있을 수 없는 일].
                    {
                        ViewBag.InvalidToken = true;
                        ViewBag.AccountCreated = true;
                        TempData["Error"] = "Error occurred when processing about update account status";
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

        #region 비밀번호 초기화
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] //To Prevent CSRF attack
        public async Task<IActionResult> ForgotPassword(LoginInputViewModel loginInputViewModel)
        {
            ModelState.Remove(nameof(loginInputViewModel.FullName));
            ModelState.Remove(nameof(loginInputViewModel.Password));

            if (ModelState.IsValid)
            {
                var account = await accountRepository.GetAccountAsync(loginInputViewModel.Email);

                if (account == null) // 입력 된 Email로 생성된 Record 없다.
                {
                    ViewBag.mailSent = true;
                    ViewBag.sentEmailAddress = loginInputViewModel.Email;
                    return View();
                }

                if (!account.EmailConfirmed) // 이메일 인증 안 됨
                {
                    ViewBag.mailSent = true;
                    ViewBag.sentEmailAddress = loginInputViewModel.Email;
                    return View();
                }

                if (account.EmailConfirmed) // 이메일 인증 됨
                {
                    string resetPasswordToken = GUIDToken.Generate();
                    try
                    {
                        #region 메일 전송
                        Mail mail = new Mail();
                        mail.Subject = "MyLaboratory Reset Password";
                        mail.Body = mailRepository.GetMailResetPasswordBody(new LoginInputViewModel() { ResetPasswordToken = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(resetPasswordToken) });
                        mail.ToMailIds = new List<string>
                            {
                                account.Email
                            };
                        string result = await mailRepository.SendMail(mail);
                        if (result != AccountMessage.MailSent) // 메일 전송 오류 발생 했을 시..
                        {
                            Console.WriteLine(result); // ActivityLog 등으로 로그 남겨야 한다..
                            account.Updated = DateTime.UtcNow;
                            account.Message = AccountMessage.FailToMailSent;
                            await accountRepository.UpdateAccountAsync(account);
                            ViewBag.mailSent = true;
                            ViewBag.sentEmailAddress = loginInputViewModel.Email;
                            return View();
                        }
                        #endregion

                        account.ResetPasswordToken = resetPasswordToken;
                        account.Locked = false;
                        account.Updated = DateTime.UtcNow;
                        account.Message = AccountMessage.ResetPasswordMail;
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
                ViewBag.mailSent = true;
                ViewBag.sentEmailAddress = loginInputViewModel.Email;
                return View();
            }
            else // 입력 값 잘못 됨
            {
                return RedirectToAction("ForgotPassword", "Account");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string resetPasswordToken)
        {
            byte[] resultByte = null;
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                var loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("Index", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("Index", "UserDashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
                }
            }
            else // 로그인 안 된 상태라면
            {
                Account account = null;

                #region 1.	입력된 token[암호화 됨]이랑 모든 계정에 저장된 토큰 해쉬화 후 비교해서 해당 계정 찾아낸다.
                var rsaHelper = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey);

                foreach (var tempAccount in await accountRepository.GetAllAsync())
                {
                    try
                    {
                        if (rsaHelper.Verify(tempAccount.ResetPasswordToken, resetPasswordToken)) // 암호화된 토큰과 DB 사용자 토큰이 같다
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

                #region 2.	해당 계정을 Token으로 찾지 못했다.
                if (account == null)
                {
                    ViewBag.FailToResetPassword = true;
                    return View();
                }
                #endregion

                #region 3.	해당 계정의 Token 만료 되었다.
                if (!GUIDToken.IsTokenAlive(account.ResetPasswordToken))
                {
                    ViewBag.FailToResetPassword = true;
                    return View();
                }
                #endregion

                #region 4.	해당 계정의 EmailConfirmd가 false이다. (이메일 인증을 하지 않았다.)
                if (!account.EmailConfirmed)
                {
                    ViewBag.FailToResetPassword = true;
                    return View();
                }
                #endregion

                #region 5.	해당 계정의 EmailConfirmd가 true이다. (이메일 인증을 하였다.)
                else
                {
                    ViewBag.FailToResetPassword = false;
                    ViewBag.ResetPasswordToken = resetPasswordToken;
                    return View();
                }
                #endregion
            }
        }
        #endregion
    }
}