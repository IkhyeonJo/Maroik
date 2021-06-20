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
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "DashBoard");
            }
            else
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