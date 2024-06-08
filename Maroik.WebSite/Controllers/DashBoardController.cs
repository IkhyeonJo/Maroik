using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
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
        private readonly ILogger<DashBoardController> _logger;
        private readonly IIncomeRepository _incomeRepository;
        private readonly IExpenditureRepository _expenditureRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly IAccountRepository _accountRepository;

        public DashBoardController(ILogger<DashBoardController> logger, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IAssetRepository assetRepository, IAccountRepository accountRepository)
        {
            _logger = logger;
            _incomeRepository = incomeRepository;
            _expenditureRepository = expenditureRepository;
            _assetRepository = assetRepository;
            _accountRepository = accountRepository;
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

        #region Read
        [HttpGet]
        public async Task<IActionResult> UserIndex(string year, string month)
        {
            _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            UserIndexOutputViewModel userIndexOutputViewModel = new();
            Account currentLoginUser = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
            List<Asset> assets = (await _assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)) ?? [];

            #region Year & Month
            if (string.IsNullOrEmpty(year))
            {
                year = DateTime.UtcNow.Year.ToString();
            }
            else
            {
                try
                {
                    year = Convert.ToInt32(year).ToString();
                }
                catch
                {
                    year = DateTime.UtcNow.Year.ToString();
                }
            }

            if (string.IsNullOrEmpty(month))
            {
                month = DateTime.UtcNow.Month.ToString("00");
            }
            else
            {
                try
                {
                    month = Convert.ToInt32(month).ToString("00");
                }
                catch
                {
                    month = DateTime.UtcNow.Month.ToString("00");
                }
            }
            #endregion

            #region Set if currentLoginUser's DefaultMonetaryUnit is not existed in Assets
            if (currentLoginUser?.DefaultMonetaryUnit == null && assets.Count() == 0)
            {
                currentLoginUser.DefaultMonetaryUnit = null;
                await _accountRepository.UpdateAccountAsync(currentLoginUser);
            }
            else if (currentLoginUser?.DefaultMonetaryUnit == null && assets.Count() != 0)
            {
                IGrouping<string, Asset> mostUsedMonetaryUnit = assets
                    .GroupBy(a => a.MonetaryUnit)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();
                currentLoginUser.DefaultMonetaryUnit = mostUsedMonetaryUnit?.Key;
                await _accountRepository.UpdateAccountAsync(currentLoginUser);
            }
            else if (currentLoginUser?.DefaultMonetaryUnit != null && assets.Count() == 0)
            {
                currentLoginUser.DefaultMonetaryUnit = null;
                await _accountRepository.UpdateAccountAsync(currentLoginUser);
            }
            else if (currentLoginUser?.DefaultMonetaryUnit != null && assets.Count() != 0)
            {
                if (!assets.Select(x => x.MonetaryUnit).Distinct().ToList().Contains(currentLoginUser.DefaultMonetaryUnit))
                {
                    IGrouping<string, Asset> mostUsedMonetaryUnit = assets
                        .GroupBy(a => a.MonetaryUnit)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault();
                    currentLoginUser.DefaultMonetaryUnit = mostUsedMonetaryUnit?.Key;
                    await _accountRepository.UpdateAccountAsync(currentLoginUser);
                }
            }

            userIndexOutputViewModel.DefaultMonetaryUnit = currentLoginUser.DefaultMonetaryUnit;
            #endregion

            #region Current Year & Month Income/Expenditure & MonetaryUnits
            if (userIndexOutputViewModel.DefaultMonetaryUnit != null)
            {
                string currentYearMonth = $"{year}-{month}";
                string currentLoginUserEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;

                List<Income> incomes = await _incomeRepository.GetCurrentYearMonthIncomesAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId);
                userIndexOutputViewModel.IncomeOutputViewModels = [];
                foreach (Income item in incomes)
                {
                    userIndexOutputViewModel.IncomeOutputViewModels.Add(new IncomeOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = item.MainClass,
                        SubClass = item.SubClass,
                        Contents = item.Contents,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note
                    });
                }

                List<Expenditure> expenditures = await _expenditureRepository.GetCurrentYearMonthExpendituresAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId);
                userIndexOutputViewModel.ExpenditureOutputViewModels = [];
                foreach (Expenditure item in expenditures)
                {
                    userIndexOutputViewModel.ExpenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = item.MainClass,
                        SubClass = item.SubClass,
                        Contents = item.Contents,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit,
                        PaymentMethod = item.PaymentMethod,
                        Note = item.Note,
                        MyDepositAsset = item.MyDepositAsset,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                    });
                }
                userIndexOutputViewModel.IncomeOutputViewModels = userIndexOutputViewModel.IncomeOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
                userIndexOutputViewModel.ExpenditureOutputViewModels = userIndexOutputViewModel.ExpenditureOutputViewModels.Where(x => x.MonetaryUnit == userIndexOutputViewModel.DefaultMonetaryUnit).ToList();
                userIndexOutputViewModel.USP_Income_MonthlyReport_Results = await _incomeRepository.GetCurrentYearMonthIncomesMonthlyReportAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId, userIndexOutputViewModel.DefaultMonetaryUnit);
                userIndexOutputViewModel.USP_Expenditure_MonthlyReport_Results = await _expenditureRepository.GetCurrentYearMonthExpendituresMonthlyReportAsync(currentLoginUserEmail, currentYearMonth, JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId, userIndexOutputViewModel.DefaultMonetaryUnit);
            }
            #endregion

            #region Set MonetaryUnits
            if (assets?.Count() != 0)
            {
                userIndexOutputViewModel.MonetaryUnits = assets?.Select(x => x?.MonetaryUnit)?.Distinct()?.ToList() ?? [];
            }
            #endregion

            #region StartYear
            Income firstCreatedIncome = (await _incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)).OrderBy(x => x.Created).ToList().FirstOrDefault() ?? new Income();
            Expenditure firstCreatedExpenditure = (await _expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)).OrderBy(x => x.Created).ToList().FirstOrDefault() ?? new Expenditure();

            if (firstCreatedIncome.Created == DateTime.MinValue)
            {
                firstCreatedIncome.Created = DateTime.MaxValue;
            }

            if (firstCreatedExpenditure.Created == DateTime.MinValue)
            {
                firstCreatedExpenditure.Created = DateTime.MaxValue;
            }

            DateTime firstCreatedIncomeOrExpenditure = DateTime.Compare(firstCreatedIncome.Created, firstCreatedExpenditure.Created) < 0
                ? firstCreatedIncome.Created
                : firstCreatedExpenditure.Created;
            if (firstCreatedIncomeOrExpenditure == DateTime.MaxValue)
            {
                firstCreatedIncomeOrExpenditure = DateTime.UtcNow;
            }

            firstCreatedIncomeOrExpenditure = firstCreatedIncomeOrExpenditure.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
            #endregion

            ViewBag.StartYear = firstCreatedIncomeOrExpenditure.Year.ToString();
            ViewBag.SelectedYear = year;
            ViewBag.SelectedMonth = month;

            return View(userIndexOutputViewModel);
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UserUpdateDefaultMonetary([FromBody] UserIndexInputViewModel userIndexInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                List<Asset> assets = (await _assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)) ?? [];

                Account currentLoginUser = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                currentLoginUser.DefaultMonetaryUnit = !assets.Select(x => x.MonetaryUnit).Distinct().ToList().Contains(userIndexInputViewModel.DefaultMonetaryUnit)
                    ? null
                    : userIndexInputViewModel.DefaultMonetaryUnit;

                await _accountRepository.UpdateAccountAsync(currentLoginUser);

                return Json(new { result = true });

            }
            catch
            {
                return Json(new { result = false });
            }
        }
        #endregion

        #endregion

        #region Anonymous
        [HttpGet]
        public IActionResult AnonymousIndex()
        {
            bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

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
