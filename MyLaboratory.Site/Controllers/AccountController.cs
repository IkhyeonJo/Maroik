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
                    if (account == null) // 입력된 ID로 생성된 Record가 없다.
                    {
                        string guid = GUIDToken.Generate();

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
                                Guid = guid,
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Message = AccountMessage.UserCreatedVerifyEmail
                            }
                            );
                        //메일 전송...
                    }
                    return View();

                }
                else
                {
                    return RedirectToAction("Register", "Account");
                }
            }
            else
            {
                return View();
            }
        }
        #endregion
    }
}