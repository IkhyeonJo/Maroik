using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models.ViewModels.Account;
using Maroik.WebSite.Models.ViewModels.DashBoard;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class DashBoardController : Controller
    {
        private readonly ILogger<DashBoardController> logger;
        private readonly IIncomeRepository incomeRepository;
        private readonly IExpenditureRepository expenditureRepository;

        public DashBoardController(ILogger<DashBoardController> logger, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository)
        {
            this.logger = logger;
            this.incomeRepository = incomeRepository;
            this.expenditureRepository = expenditureRepository;
        }

        #region 언어 변경
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        [RequiredHttpPostAccess(Role = Role.Anonymous)]
        public IActionResult CultureManagement([FromBody] LoginInputViewModel loginInputViewModel)
        {
            Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName, CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(loginInputViewModel.Culture)),
                new CookieOptions { Expires = DateTimeOffset.Now.AddDays(30), Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict });

            return Json(new { result = true });
        }
        #endregion

        #region Admin
        [HttpGet]
        public IActionResult AdminIndex()
        {
            return View();
        }
        #endregion

        #region User
        [HttpGet]
        public async Task<IActionResult> UserIndex()
        {
            #region Current Year & Month Income/Expenditure
            _ = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
            string currentYearMonth = DateTime.UtcNow.ToString("yyyy-MM");
            string currentLoginUserEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;

            UserIndexOutputViewModel userIndexOutputViewModel = new()
            {
                Incomes = await incomeRepository.GetCurrentYearMonthIncomesAsync(currentLoginUserEmail, currentYearMonth),
                Expenditures = await expenditureRepository.GetCurrentYearMonthExpendituresAsync(currentLoginUserEmail, currentYearMonth),
                USP_Income_MonthlyReport_Results = await incomeRepository.GetCurrentYearMonthIncomesMonthlyReport(currentLoginUserEmail, currentYearMonth)
            };
            ;
            userIndexOutputViewModel.USP_Expenditure_MonthlyReport_Results = await expenditureRepository.GetCurrentYearMonthExpendituresMonthlyReport(currentLoginUserEmail, currentYearMonth);
            #endregion

            return View(userIndexOutputViewModel);
        }
        #endregion

        #region Anonymous
        [HttpGet]
        public IActionResult AnonymousIndex()
        {
            bool isAccountSessionExist = HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

            if (isAccountSessionExist) // 로그인 된 상태라면
            {
                Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                if (loginedAccount.Role == Role.Admin) // 로그인한 사용자 권한이 Admin일 경우
                {
                    return RedirectToAction("AdminIndex", "DashBoard");
                }
                else if (loginedAccount.Role == Role.User) // 로그인한 사용자 권한이 User일 경우
                {
                    return RedirectToAction("UserIndex", "DashBoard");
                }
                else // 로그인한 사용자 권한이 Admin도 아니고 User도 아니면 DB Insert 오류임
                {
                    return null; // 잘못된 값을 넣으면 잘못된 값이 나옴
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
