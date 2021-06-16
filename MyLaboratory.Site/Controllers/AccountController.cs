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

namespace MyLaboratory.Site.Controllers
{

    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> logger;
        private readonly IAccountRepository accountRepository;

        public AccountController(ILogger<AccountController> logger, IAccountRepository accountRepository)
        {
            this.logger = logger;
            this.accountRepository = accountRepository;
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
        public async Task<IActionResult> Login(LoginInputViewModel loginInputViewModel)
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

                var claims = new List<Claim>();
                claims.Add(new Claim("email", loginInputViewModel.Email));
                claims.Add(new Claim(ClaimTypes.NameIdentifier, loginInputViewModel.Email));
                claims.Add(new Claim(ClaimTypes.Name, $"{tempAccount?.FirstName}{tempAccount?.LastName}"));
                claims.Add(new Claim(ClaimTypes.Role, tempAccount.Role));
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimPrincipal = new ClaimsPrincipal(claimsIdentity);
                await HttpContext.SignInAsync(claimPrincipal);
                return RedirectToAction("Index", "DashBoard");
            }
            else
            {
                return RedirectToAction("Index", "DashBoard");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
