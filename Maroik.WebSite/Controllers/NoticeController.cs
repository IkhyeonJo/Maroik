using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Models.ViewModels.Notice;
using Maroik.WebSite.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers
{
    public class NoticeController : Controller
    {
        private readonly IHtmlLocalizer<NoticeController> localizer;
        private readonly ILogger<NoticeController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly IHostEnvironment hostEnvironment;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        private readonly IAssetRepository assetRepository;
        private readonly IIncomeRepository incomeRepository;
        private readonly IExpenditureRepository expenditureRepository;
        private readonly IFixedIncomeRepository fixedIncomeRepository;
        private readonly IFixedExpenditureRepository fixedExpenditureRepository;
        public NoticeController(IHtmlLocalizer<NoticeController> localizer, ILogger<NoticeController> logger, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IAssetRepository assetRepository, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository, IFixedIncomeRepository fixedIncomeRepository, IFixedExpenditureRepository fixedExpenditureRepository)
        {
            this.localizer = localizer;
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
            this.assetRepository = assetRepository;
            this.incomeRepository = incomeRepository;
            this.expenditureRepository = expenditureRepository;
            this.fixedIncomeRepository = fixedIncomeRepository;
            this.fixedExpenditureRepository = fixedExpenditureRepository;
        }

        #region 고정 수입

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateFixedIncome([FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        fixedIncomeInputViewModel.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                        #region Validation of MainClass & SubClass Value
                        if (fixedIncomeInputViewModel.MainClass == "RegularIncome")
                        {
                            if (fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.MainClass == "IrregularIncome")
                        {
                            if (fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region Validate of DepositMonth
                        if (fixedIncomeInputViewModel.DepositMonth is not (>= 1 and <= 12))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        #region Validate of DepositDay
                        if (fixedIncomeInputViewModel.DepositMonth is 1 or
                            3 or
                            5 or
                            7 or
                            8 or
                            10 or
                            12)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 31))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth == 2)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 29))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth is 4 or
                                6 or
                                9 or
                                11)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 30))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region MaturityDate 형식 체크 (윤년 또는 월에 따른 일도 체크함)
                        if (!new Regex(@"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$").IsMatch(fixedIncomeInputViewModel.MaturityDate)) // https://stackoverflow.com/questions/5247219/regular-expression-to-detect-yyyy-mm-dd/15233484
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        #region 고정 수입 생성
                        await fixedIncomeRepository.CreateFixedIncomeAsync
                            (
                            new FixedIncome()
                            {
                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = fixedIncomeInputViewModel.MainClass,
                                SubClass = fixedIncomeInputViewModel.SubClass,
                                Contents = fixedIncomeInputViewModel.Contents ?? "",
                                Amount = Math.Abs(fixedIncomeInputViewModel.Amount),
                                DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
                                DepositMonth = fixedIncomeInputViewModel.DepositMonth,
                                DepositDay = fixedIncomeInputViewModel.DepositDay,
                                MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[2].ToString())),
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Note = fixedIncomeInputViewModel.Note ?? ""
                            }
                            );
                        #endregion

                        return Json(new { result = true, message = localizer["The fixedIncome has been successfully created."].Value });
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> FixedIncome(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<FixedIncomeOutputViewModel> fixedIncomeOutputViewModels = [];

                #region FixedIncomes
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

                DateTime currentDate = new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

                List<Asset> assets = await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                foreach (FixedIncome item in await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        bool noticedResult = false;

                        try
                        {
                            noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                        }
                        catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                        {
                            noticedResult = false;
                        }

                        if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                        {
                            noticedResult = true;
                        }

                        fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Contents = item.Contents,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Note = item.Note,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                    else
                    {
                        if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                            (assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch))
                        {
                            bool noticedResult = false;

                            try
                            {
                                noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                            }
                            catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                            {
                                noticedResult = false;
                            }

                            if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                            {
                                noticedResult = true;
                            }

                            fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                            {
                                Id = item.Id,
                                MainClass = localizer[item.MainClass.ToString()].Value,
                                SubClass = localizer[item.SubClass.ToString()].Value,
                                Contents = item.Contents,
                                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                                MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit,
                                DepositMonth = item.DepositMonth,
                                DepositDay = item.DepositDay,
                                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                                Note = item.Note,
                                DepositMyAssetProductName = item.DepositMyAssetProductName,
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                                Noticed = noticedResult,
                                Unpunctuality = item.Unpunctuality,
                                Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                            });
                        }
                    }
                }
                #endregion

                IQueryable<FixedIncomeOutputViewModel> result = fixedIncomeOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed).ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created).ThenByDescending(a => a.Updated).AsQueryable();
                return PartialView("_FixedIncomeGrid", result);
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsFixedIncomeExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                List<FixedIncome> tempFixedIncomes = await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempFixedIncomes == null)
                {
                    return Json(new { result = false, error = localizer["No fixedIncome exists"].Value });
                }
                else
                {
                    FixedIncome tempFixedIncome = tempFixedIncomes.Where(a => a.Id == id).FirstOrDefault();

                    if (tempFixedIncome == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        FixedIncomeOutputViewModel tempFixedIncomeOutputModel = new()
                        {
                            Id = tempFixedIncome.Id,
                            MainClass = tempFixedIncome.MainClass,
                            SubClass = tempFixedIncome.SubClass,
                            Contents = tempFixedIncome.Contents,
                            Amount = Convert.ToDecimal(tempFixedIncome.Amount.TrimTrailingZeros()),
                            DepositMonth = tempFixedIncome.DepositMonth,
                            DepositDay = tempFixedIncome.DepositDay,
                            MaturityDate = tempFixedIncome.MaturityDate.ToString("yyyy-MM-dd"),
                            Note = tempFixedIncome.Note,
                            DepositMyAssetProductName = tempFixedIncome.DepositMyAssetProductName,
                            Unpunctuality = tempFixedIncome.Unpunctuality
                        };
                        return Json(new { result = true, fixedIncome = tempFixedIncomeOutputModel });
                    }
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> GetFixedIncomeAmountLabel(string productName)
        {
            try
            {
                string label = localizer["Amount"].Value;

                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                Asset asset = (await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)).Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new Asset();

                if (!string.IsNullOrEmpty(asset.MonetaryUnit))
                {
                    label = $"{localizer["Amount"].Value} ({asset.MonetaryUnit})";
                }

                return Json(new { result = true, label = label });
            }
            catch
            {
                return Json(new { result = false, label = localizer["Amount"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateFixedIncome([FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        fixedIncomeInputViewModel.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                        #region Validation of MainClass & SubClass Value
                        if (fixedIncomeInputViewModel.MainClass == "RegularIncome")
                        {
                            if (fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.MainClass == "IrregularIncome")
                        {
                            if (fixedIncomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region Validate of DepositMonth
                        if (fixedIncomeInputViewModel.DepositMonth is not (>= 1 and <= 12))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        #region Validate of DepositDay
                        if (fixedIncomeInputViewModel.DepositMonth is 1 or
                            3 or
                            5 or
                            7 or
                            8 or
                            10 or
                            12)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 31))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth == 2)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 29))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth is 4 or
                                6 or
                                9 or
                                11)
                        {
                            if (fixedIncomeInputViewModel.DepositDay is not (>= 1 and <= 30))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region MaturityDate 형식 체크 (윤년 또는 월에 따른 일도 체크함)
                        if (!new Regex(@"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$").IsMatch(fixedIncomeInputViewModel.MaturityDate)) // https://stackoverflow.com/questions/5247219/regular-expression-to-detect-yyyy-mm-dd/15233484
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        #region 고정 수입 업데이트
                        List<FixedIncome> tempFixedIncomes = await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                        if (tempFixedIncomes == null)
                        {
                            return Json(new { result = false, error = localizer["No fixedIncome exists"].Value });
                        }
                        else
                        {
                            FixedIncome tempFixedIncome = tempFixedIncomes.Where(a => a.Id == fixedIncomeInputViewModel.Id).FirstOrDefault();

                            if (tempFixedIncome == null)
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                            else
                            {
                                #region 고정 수입 업데이트
                                tempFixedIncome.MainClass = fixedIncomeInputViewModel.MainClass;
                                tempFixedIncome.SubClass = fixedIncomeInputViewModel.SubClass;
                                tempFixedIncome.Contents = fixedIncomeInputViewModel.Contents ?? "";
                                tempFixedIncome.Amount = Math.Abs(fixedIncomeInputViewModel.Amount);
                                tempFixedIncome.DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName;
                                tempFixedIncome.DepositMonth = fixedIncomeInputViewModel.DepositMonth;
                                tempFixedIncome.DepositDay = fixedIncomeInputViewModel.DepositDay;
                                tempFixedIncome.MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedIncomeInputViewModel.MaturityDate, "-")[2].ToString()));
                                tempFixedIncome.Updated = DateTime.UtcNow;
                                tempFixedIncome.Note = fixedIncomeInputViewModel.Note ?? "";
                                tempFixedIncome.Unpunctuality = fixedIncomeInputViewModel.Unpunctuality;

                                await fixedIncomeRepository.UpdateFixedIncomeAsync(tempFixedIncome);
                                #endregion

                                return Json(new { result = true, message = localizer["The fixedIncome has been successfully updated."].Value });
                            }
                        }
                        #endregion
                    }
                    catch
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteFixedIncome([FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                List<FixedIncome> tempFixedIncomes = await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempFixedIncomes == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    FixedIncome tempFixedIncome = tempFixedIncomes.Where(a => a.Id == fixedIncomeInputViewModel.Id).FirstOrDefault();

                    if (tempFixedIncome == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        #region 고정 수입 삭제
                        await fixedIncomeRepository.DeleteFixedIncomeAsync(tempFixedIncome);
                        #endregion

                        return Json(new { result = true, message = localizer["The fixedIncome has been successfully deleted."].Value });
                    }
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> ExportExcelFixedIncome(string fileName = "")
        {
            // query data from database
            await Task.Yield();

            List<FixedIncomeOutputViewModel> fixedIncomeOutputViewModels = [];

            #region FixedIncomes

            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            DateTime currentDate = new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            List<Asset> assets = await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

            foreach (FixedIncome item in await fixedIncomeRepository.GetFixedIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                bool noticedResult = false;

                try
                {
                    noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                }
                catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                {
                    noticedResult = false;
                }

                if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                {
                    noticedResult = true;
                }

                fixedIncomeOutputViewModels.Add(new FixedIncomeOutputViewModel()
                {
                    MainClass = localizer[item.MainClass.ToString()].Value,
                    SubClass = localizer[item.SubClass.ToString()].Value,
                    Contents = item.Contents,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit,
                    DepositMonth = item.DepositMonth,
                    DepositDay = item.DepositDay,
                    MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                    Note = item.Note,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                    Noticed = noticedResult,
                    Unpunctuality = item.Unpunctuality,
                    Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                });
            }
            fixedIncomeOutputViewModels = fixedIncomeOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed).ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created).ThenByDescending(a => a.Updated).ToList();
            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(FixedIncomeOutputViewModel.MainClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(FixedIncomeOutputViewModel.SubClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(FixedIncomeOutputViewModel.Contents).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(FixedIncomeOutputViewModel.Amount).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(FixedIncomeOutputViewModel.MonetaryUnit).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(FixedIncomeOutputViewModel.DepositMonth).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(FixedIncomeOutputViewModel.DepositDay).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(FixedIncomeOutputViewModel.MaturityDate).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(FixedIncomeOutputViewModel.Note).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 10].Value = localizer[nameof(FixedIncomeOutputViewModel.DepositMyAssetProductName).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 11].Value = localizer[nameof(FixedIncomeOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 12].Value = localizer[nameof(FixedIncomeOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 13].Value = localizer[nameof(FixedIncomeOutputViewModel.Noticed).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 14].Value = localizer[nameof(FixedIncomeOutputViewModel.Expired).ToString()].Value;

                int recordIndex = 2;
                foreach (FixedIncomeOutputViewModel item in fixedIncomeOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(a => a.Noticed))
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Contents;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = Convert.ToDecimal(item.Amount.TrimTrailingZeros());
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault().MonetaryUnit;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.DepositMonth;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.DepositDay;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.MaturityDate;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.Note;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 10].Value = item.DepositMyAssetProductName;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 11].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 12].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 13].Value = item.Noticed.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 14].Value = item.Expired.ToString();
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion

        #region 고정 지출

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateFixedExpenditure([FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    fixedExpenditureInputViewModel.Amount = Math.Abs(fixedExpenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                    #region 대분류 = '정기저축'
                    if (fixedExpenditureInputViewModel.MainClass == "RegularSavings")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "Deposit" or "Investment")
                        {
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod == fixedExpenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                                {
                                    return Json(new { result = false, error = localizer["PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."].Value });
                                }

                                #region 고정 지출 생성
                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                    (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Contents = fixedExpenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset,
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString())),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully created."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류 = '비소비지출'
                    else if (fixedExpenditureInputViewModel.MainClass == "NonConsumerSpending")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                        {
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod == fixedExpenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                                {
                                    return Json(new { result = false, error = localizer["PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."].Value });
                                }

                                #region 고정 지출 생성
                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                    (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Contents = fixedExpenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset,
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString())),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully created."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedExpenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer")
                        {
                            try
                            {
                                #region 고정 지출 생성
                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                    (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Contents = fixedExpenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString())),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully created."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류 = '소비지출'
                    else if (fixedExpenditureInputViewModel.MainClass == "ConsumerSpending")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
                            "HousingOrSuppliesCost" or
                            "EducationExpenses" or
                            "MedicalExpenses" or
                            "TransportationCost" or
                            "CommunicationCost" or
                            "LeisureOrCulture" or
                            "ClothingOrShoes" or
                            "PinMoney" or
                            "ProtectionTypeInsurance" or
                            "OtherExpenses" or
                            "UnknownExpenditure")
                        {
                            try
                            {
                                #region 고정 지출 생성
                                await fixedExpenditureRepository.CreateFixedExpenditureAsync
                                    (
                                    new FixedExpenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = fixedExpenditureInputViewModel.MainClass,
                                        SubClass = fixedExpenditureInputViewModel.SubClass,
                                        Contents = fixedExpenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(fixedExpenditureInputViewModel.Amount),
                                        PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        DepositMonth = fixedExpenditureInputViewModel.DepositMonth,
                                        DepositDay = fixedExpenditureInputViewModel.DepositDay,
                                        MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString())),
                                        Created = DateTime.UtcNow,
                                        Updated = DateTime.UtcNow,
                                        Note = fixedExpenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully created."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류에 없는 값
                    else
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    #endregion
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> FixedExpenditure(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels = [];

                #region FixedExpenditures
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

                DateTime currentDate = new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

                List<Asset> assets = await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                foreach (FixedExpenditure item in await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        bool noticedResult = false;

                        try
                        {
                            noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                        }
                        catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                        {
                            noticedResult = false;
                        }

                        if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                        {
                            noticedResult = true;
                        }

                        fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Contents = item.Contents,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit,
                            PaymentMethod = item.PaymentMethod,
                            MyDepositAsset = item.MyDepositAsset,
                            DepositMonth = item.DepositMonth,
                            DepositDay = item.DepositDay,
                            MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                            Note = item.Note,
                            Noticed = noticedResult,
                            Unpunctuality = item.Unpunctuality,
                            Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                        });
                    }
                    else
                    {
                        if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                            (assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit.ToString() ?? "").Contains(wholeSearch) ||
                            (item.PaymentMethod.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositMonth.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositDay.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MaturityDate.ToString("yyyy-MM-dd") ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch))
                        {
                            bool noticedResult = false;

                            try
                            {
                                noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                            }
                            catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                            {
                                noticedResult = false;
                            }

                            if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                            {
                                noticedResult = true;
                            }

                            fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                            {
                                Id = item.Id,
                                MainClass = localizer[item.MainClass.ToString()].Value,
                                SubClass = localizer[item.SubClass.ToString()].Value,
                                Contents = item.Contents,
                                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                                MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit,
                                PaymentMethod = item.PaymentMethod,
                                MyDepositAsset = item.MyDepositAsset,
                                DepositMonth = item.DepositMonth,
                                DepositDay = item.DepositDay,
                                MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                                Note = item.Note,
                                Noticed = noticedResult,
                                Unpunctuality = item.Unpunctuality,
                                Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                            });
                        }
                    }
                }
                #endregion

                IQueryable<FixedExpenditureOutputViewModel> result = fixedExpenditureOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed).ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created).ThenByDescending(a => a.Updated).AsQueryable();
                return PartialView("_FixedExpenditureGrid", result);
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsFixedExpenditureExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempFixedExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["No fixedExpenditure exists"].Value });
                }
                else
                {
                    FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == id).FirstOrDefault();

                    if (tempFixedExpenditure == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        FixedExpenditureOutputViewModel tempFixedIncomeOutputModel = new()
                        {
                            Id = tempFixedExpenditure.Id,
                            MainClass = tempFixedExpenditure.MainClass,
                            SubClass = tempFixedExpenditure.SubClass,
                            Contents = tempFixedExpenditure.Contents,
                            Amount = Convert.ToDecimal(tempFixedExpenditure.Amount.TrimTrailingZeros()),
                            DepositMonth = tempFixedExpenditure.DepositMonth,
                            DepositDay = tempFixedExpenditure.DepositDay,
                            MaturityDate = tempFixedExpenditure.MaturityDate.ToString("yyyy-MM-dd"),
                            Note = tempFixedExpenditure.Note,
                            PaymentMethod = tempFixedExpenditure.PaymentMethod,
                            MyDepositAsset = tempFixedExpenditure.MyDepositAsset ?? "",
                            Unpunctuality = tempFixedExpenditure.Unpunctuality
                        };
                        return Json(new { result = true, fixedExpenditure = tempFixedIncomeOutputModel });
                    }
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> GetFixedExpenditureAmountLabel(string productName)
        {
            try
            {
                string label = localizer["Amount"].Value;

                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                Asset asset = (await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email)).Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new Asset();

                if (!string.IsNullOrEmpty(asset.MonetaryUnit))
                {
                    label = $"{localizer["Amount"].Value} ({asset.MonetaryUnit})";
                }

                return Json(new { result = true, label = label });
            }
            catch
            {
                return Json(new { result = false, label = localizer["Amount"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateFixedExpenditure([FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    fixedExpenditureInputViewModel.Amount = Math.Abs(fixedExpenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                    #region 대분류 = '정기저축'
                    if (fixedExpenditureInputViewModel.MainClass == "RegularSavings")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "Deposit" or "Investment")
                        {
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod == fixedExpenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                                {
                                    return Json(new { result = false, error = localizer["PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."].Value });
                                }

                                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == fixedExpenditureInputViewModel.Id).FirstOrDefault();

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Contents = fixedExpenditureInputViewModel.Contents ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString()));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel?.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully updated."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류 = '비소비지출'
                    else if (fixedExpenditureInputViewModel.MainClass == "NonConsumerSpending")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                        {
                            try
                            {
                                if (fixedExpenditureInputViewModel.PaymentMethod == fixedExpenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedExpenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                                {
                                    return Json(new { result = false, error = localizer["PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."].Value });
                                }

                                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == fixedExpenditureInputViewModel.Id).FirstOrDefault();

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Contents = fixedExpenditureInputViewModel.Contents ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString()));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel?.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully updated."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedExpenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer")
                        {
                            try
                            {
                                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == fixedExpenditureInputViewModel.Id).FirstOrDefault();

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Contents = fixedExpenditureInputViewModel.Contents ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString()));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel?.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully updated."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류 = '소비지출'
                    else if (fixedExpenditureInputViewModel.MainClass == "ConsumerSpending")
                    {
                        if (fixedExpenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
                            "HousingOrSuppliesCost" or
                            "EducationExpenses" or
                            "MedicalExpenses" or
                            "TransportationCost" or
                            "CommunicationCost" or
                            "LeisureOrCulture" or
                            "ClothingOrShoes" or
                            "PinMoney" or
                            "ProtectionTypeInsurance" or
                            "OtherExpenses" or
                            "UnknownExpenditure")
                        {
                            try
                            {
                                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == fixedExpenditureInputViewModel.Id).FirstOrDefault();

                                if (tempFixedExpenditure == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                tempFixedExpenditure.Id = fixedExpenditureInputViewModel.Id;
                                tempFixedExpenditure.AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email;
                                tempFixedExpenditure.MainClass = fixedExpenditureInputViewModel.MainClass;
                                tempFixedExpenditure.SubClass = fixedExpenditureInputViewModel.SubClass;
                                tempFixedExpenditure.Contents = fixedExpenditureInputViewModel.Contents ?? "";
                                tempFixedExpenditure.Amount = fixedExpenditureInputViewModel.Amount;
                                tempFixedExpenditure.DepositMonth = fixedExpenditureInputViewModel.DepositMonth;
                                tempFixedExpenditure.DepositDay = fixedExpenditureInputViewModel.DepositDay;
                                tempFixedExpenditure.MaturityDate = new DateTime(Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[0].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[1].ToString()), Convert.ToInt32(Regex.Split(fixedExpenditureInputViewModel.MaturityDate, "-")[2].ToString()));
                                tempFixedExpenditure.Note = fixedExpenditureInputViewModel?.Note ?? "";
                                tempFixedExpenditure.PaymentMethod = fixedExpenditureInputViewModel.PaymentMethod;
                                tempFixedExpenditure.MyDepositAsset = "";
                                tempFixedExpenditure.Updated = DateTime.UtcNow;
                                tempFixedExpenditure.Unpunctuality = fixedExpenditureInputViewModel.Unpunctuality;

                                await fixedExpenditureRepository.UpdateFixedExpenditureAsync(tempFixedExpenditure);

                                return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully updated."].Value });
                            }
                            catch
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                    }
                    #endregion

                    #region 대분류에 없는 값
                    else
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    #endregion
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteFixedExpenditure([FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

                List<FixedExpenditure> tempFixedExpenditures = await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempFixedExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    FixedExpenditure tempFixedExpenditure = tempFixedExpenditures.Where(a => a.Id == fixedExpenditureInputViewModel.Id).FirstOrDefault();
                    if (tempFixedExpenditure == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        await fixedExpenditureRepository.DeleteFixedExpenditureAsync(tempFixedExpenditure);
                        return Json(new { result = true, message = localizer["The fixedExpenditure has been successfully deleted."].Value });
                    }
                }
            }
            catch
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> ExportExcelFixedExpenditure(string fileName = "")
        {
            // query data from database
            await Task.Yield();

            List<FixedExpenditureOutputViewModel> fixedExpenditureOutputViewModels = [];

            #region FixedExpenditures

            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Extensions.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            DateTime currentDate = new(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[1]), Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[2]));

            List<Asset> assets = await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

            foreach (FixedExpenditure item in await fixedExpenditureRepository.GetFixedExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                bool noticedResult = false;

                try
                {
                    noticedResult = currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays <= 0 && Math.Abs(currentDate.Subtract(new DateTime(Convert.ToInt32(Regex.Split(DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd"), "-")[0]), item.DepositMonth, item.DepositDay)).TotalDays) <= ServerSetting.NoticeMaturityDateDay;
                }
                catch // 윤년이 아닌데 2월 29일로 초기화 했을 때 예외 발생
                {
                    noticedResult = false;
                }

                if (item.Unpunctuality) // 시간 미엄수 체크 시, 알림 뜨도록 설정
                {
                    noticedResult = true;
                }

                fixedExpenditureOutputViewModels.Add(new FixedExpenditureOutputViewModel()
                {
                    MainClass = localizer[item.MainClass.ToString()].Value,
                    SubClass = localizer[item.SubClass.ToString()].Value,
                    Contents = item.Contents,
                    Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                    MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit,
                    PaymentMethod = item.PaymentMethod,
                    MyDepositAsset = item.MyDepositAsset,
                    DepositMonth = item.DepositMonth,
                    DepositDay = item.DepositDay,
                    MaturityDate = item.MaturityDate.ToString("yyyy-MM-dd"),
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, loginedAccountTimeZoneIanaId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, loginedAccountTimeZoneIanaId),
                    Note = item.Note,
                    Noticed = noticedResult,
                    Unpunctuality = item.Unpunctuality,
                    Expired = item.MaturityDate.Subtract(currentDate).TotalDays < 0
                });
            }
            fixedExpenditureOutputViewModels = fixedExpenditureOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed).ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created).ThenByDescending(a => a.Updated).ToList();
            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(FixedExpenditureOutputViewModel.MainClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(FixedExpenditureOutputViewModel.SubClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(FixedExpenditureOutputViewModel.Contents).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(FixedExpenditureOutputViewModel.Amount).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(FixedExpenditureOutputViewModel.MonetaryUnit).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(FixedExpenditureOutputViewModel.PaymentMethod).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(FixedExpenditureOutputViewModel.MyDepositAsset).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(FixedExpenditureOutputViewModel.DepositMonth).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(FixedExpenditureOutputViewModel.DepositDay).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 10].Value = localizer[nameof(FixedExpenditureOutputViewModel.MaturityDate).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 11].Value = localizer[nameof(FixedExpenditureOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 12].Value = localizer[nameof(FixedExpenditureOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 13].Value = localizer[nameof(FixedExpenditureOutputViewModel.Note).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 14].Value = localizer[nameof(FixedExpenditureOutputViewModel.Noticed).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 15].Value = localizer[nameof(FixedExpenditureOutputViewModel.Expired).ToString()].Value;

                int recordIndex = 2;
                foreach (FixedExpenditureOutputViewModel item in fixedExpenditureOutputViewModels.OrderByDescending(a => a.Expired).ThenByDescending(a => a.Noticed))
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Contents;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = Convert.ToDecimal(item.Amount.TrimTrailingZeros());
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.PaymentMethod;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.MyDepositAsset;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.DepositMonth;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.DepositDay;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 10].Value = item.MaturityDate;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 11].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 12].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 13].Value = item.Note;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 14].Value = item.Noticed.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 15].Value = item.Expired.ToString();
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion
    }
}