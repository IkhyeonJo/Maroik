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

namespace MyLaboratory.Site.Controllers
{

    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ILogger<HomeController> logger;

        public AccountController(ILogger<HomeController> logger)
        {
            this.logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
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
            if (ModelState.IsValid)
            {
                #region Hash Password
                string hashedPassword = Crypto.HashPassword(loginInputViewModel.Password);
                var isMatched = Crypto.VerifyHashedPassword("AQAAAAEAACcQAAAAECBhgdIiE2wlfa/ORMIoj1az6uIYxb9qEKi5m1/NxbFw5eyUDEbEFHgl5dwrg4N4Yg==", loginInputViewModel.Password);
                #endregion
                if (loginInputViewModel.Email == "bob@gmail.com" && loginInputViewModel.Password== "Pa$$w0rd")
                {
                    var claims = new List<Claim>();
                    claims.Add(new Claim("email", loginInputViewModel.Email));
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, loginInputViewModel.Email));
                    claims.Add(new Claim(ClaimTypes.Name, "Bob Edward Jones"));
                    //claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var claimPrincipal = new ClaimsPrincipal(claimsIdentity);
                    await HttpContext.SignInAsync(claimPrincipal);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Error"] = "Username or Password is invalid";
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
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
        public IActionResult Denied()
        {
            return View();
        }
    }
}
