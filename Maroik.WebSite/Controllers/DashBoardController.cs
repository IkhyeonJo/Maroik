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
using System.Diagnostics;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Security.Cryptography;

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
            // CPU Model 
            string cpuModelName = ServerSetting.ExecuteBashCommand("cat /proc/cpuinfo | grep 'model name' | uniq | awk -F': ' '{print $2}'");

            // CPU 사용량 (퍼센트)
            string cpuUsage = ServerSetting.ExecuteBashCommand("ps -eo %cpu --no-headers | awk '{sum += $1} END {print sum}'");

            // 각 프로세스의 CPU 사용률 (%)
            string cpuUsageList = ServerSetting.ExecuteBashCommand("ps -eo pid,%cpu,comm --sort=-%cpu");

            // 1분, 5분, 15분 평균 부하 (Load Average)
            string loadAverage = ServerSetting.ExecuteBashCommand("uptime | awk -F 'load average:' '{ print $2 }' | sed 's/,//g'");

            // 사용 가능한 RAM (GB)
            string availableRAM = ServerSetting.ExecuteBashCommand("free -h | grep 'Mem' | awk '{print $7}'");

            // RAM 사용량 (Percentage)
            string memoryUsage = ServerSetting.ExecuteBashCommand("ps -eo %mem --no-headers | awk '{sum += $1} END {print sum}'");

            // 각 프로세스의 메모리 사용률 (%)
            string memoryUsageList = ServerSetting.ExecuteBashCommand("ps -eo pid,%mem,comm --sort=-%mem");

            // 사용 가능한 스왑 메모리 (GB)
            string availableSwap = ServerSetting.ExecuteBashCommand("free -h | grep 'Swap' | awk '{print $4}'");

            // 디스크 사용 공간 (GB)
            string diskUsageGB = ServerSetting.ExecuteBashCommand("df -BG / | grep '/' | awk '{print $3}' | sed 's/G//'");

            // 디스크 여유 공간 (GB)
            string freeDiskSpace = ServerSetting.ExecuteBashCommand("df -h / | grep '/' | awk '{print $4}'");

            // 디스크 사용량 (퍼센트)
            string diskUsage = ServerSetting.ExecuteBashCommand("df -h / | grep '/' | awk '{print $5}'");

            //Device: 디스크 장치의 이름(예: loop0, sda, sdb, sdc 등).
            //rrqm / s: 초당 읽기 요청 병합 수. (읽기 요청이 디스크에 보내지기 전에 병합된 요청 수)
            //wrqm / s: 초당 쓰기 요청 병합 수. (쓰기 요청이 병합된 수)
            //r / s: 초당 읽기 요청 수.
            //w / s: 초당 쓰기 요청 수.
            //rsec / s: 초당 읽기 섹터 수.
            //wsec / s: 초당 쓰기 섹터 수.
            //avgrq - sz: 평균 요청 크기(읽기와 쓰기 요청의 섹터 크기).
            //avgqu - sz: 평균 대기 큐 크기.
            //await: 요청이 대기하는 평균 시간(ms).
            //svctm: 서비스 시간(ms).
            //% util: 디스크가 사용되는 시간의 비율(%).

            //Device: 디스크 장치의 이름.
            //tps: 초당 전송 수. (읽기 및 쓰기 요청의 총합)
            //kB_read / s: 초당 읽은 데이터의 양(KB).
            //kB_wrtn / s: 초당 쓴 데이터의 양(KB).
            //kB_dscd / s: 초당 삭제된 데이터의 양(KB).
            //kB_read: 총 읽은 데이터(KB).
            //kB_wrtn: 총 쓴 데이터(KB).
            //kB_dscd: 총 삭제된 데이터(KB).

            // 디스크 I/O 통계 가져오기
            string diskIOStats = ServerSetting.ExecuteBashCommand("iostat -d -k 1 2 | tail -n +4 | head -n -1");

            // 네트워크 수신 속도 (KiB/s)
            string networkIn = ServerSetting.ExecuteBashCommand("cat /proc/net/dev | grep 'eth0' | awk '{print $2 / 1024}'");

            // 네트워크 송신 속도 (KiB/s)
            string networkOut = ServerSetting.ExecuteBashCommand("cat /proc/net/dev | grep 'eth0' | awk '{print $10 / 1024}'");

            ViewBag.CpuUsage = cpuUsage;
            ViewBag.CpuModelName = cpuModelName;
            ViewBag.MemoryUsage = memoryUsage;
            ViewBag.DiskUsage = diskUsage;

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
                        Content = item.Content,
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
                        Content = item.Content,
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
