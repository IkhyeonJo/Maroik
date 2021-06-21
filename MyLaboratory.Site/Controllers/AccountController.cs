using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.Site.Models.AccountViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Services;
using Newtonsoft.Json;
using System.Text;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Data;

namespace MyLaboratory.Site.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;

        public AccountController(ILogger<AccountController> logger, IAccountRepository accountRepository, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
        {
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
        }

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
        public IActionResult Login(LoginInputViewModel loginInputViewModel)
        {
            //var aa = accountRepository.LockAccount(loginInputViewModel.Email);
            if (ModelState.IsValid)
            {
                var tempAccount = accountRepository.GetAccount(loginInputViewModel.Email);

                if (tempAccount == null) // no account exist
                {
                    TempData["Error"] = "Username or Password is invalid";
                    return RedirectToAction("Index", "DashBoard");
                }

                if (!Crypto.VerifyHashedPassword(tempAccount.HashedPassword, loginInputViewModel.Password)) // wrong password
                {
                    TempData["Error"] = "Username or Password is invalid";
                    return RedirectToAction("Index", "DashBoard");
                }

                tempAccount.HashedPassword = null;

                HttpContext.Session.Set("AccountSession", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));

                #region DB Cache
                if (tempAccount.Role == Role.Admin)
                {
                    DbCache.AdminCategories = categoryRepository.AdminCategories;
                    DbCache.AdminSubCategories = subCategoryRepository.AdminSubCategories;
                }
                else if(tempAccount.Role == Role.User)
                {
                    DbCache.UserCategories = categoryRepository.UserCategories;
                    DbCache.UserSubCategories = subCategoryRepository.UserSubCategories;
                }
                #endregion

                return RedirectToAction("Index", "DashBoard");
            }
            else
            {
                return RedirectToAction("Index", "DashBoard");
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}