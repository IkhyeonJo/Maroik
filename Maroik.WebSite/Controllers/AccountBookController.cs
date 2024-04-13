using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Data;
using System.Text;

namespace Maroik.WebSite.Controllers
{
    public class AccountBookController : Controller
    {
        private readonly IHtmlLocalizer<AccountBookController> localizer;
        private readonly ILogger<AccountBookController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly IHostEnvironment hostEnvironment;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        private readonly IAssetRepository assetRepository;
        private readonly IIncomeRepository incomeRepository;
        private readonly IExpenditureRepository expenditureRepository;
        public AccountBookController(IHtmlLocalizer<AccountBookController> localizer, ILogger<AccountBookController> logger, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IAssetRepository assetRepository, IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository)
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
        }

        #region 자산

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateAsset([FromBody] AssetInputViewModel assetInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                    if (await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.ProductName) != null)
                    {
                        return Json(new { result = false, error = localizer["The asset already exists."].Value });
                    }

                    try
                    {
                        #region Validation of item Value
                        if (assetInputViewModel.Item is not ("FreeDepositAndWithdrawal" or
                            "TrustAsset" or
                            "CashAsset" or
                            "SavingsAsset" or
                            "InvestmentAsset" or
                            "RealEstate" or
                            "Movables" or
                            "OtherPhysicalAsset" or
                            "InsuranceAsset"))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        await assetRepository.CreateAssetAsync
                            (
                            new Asset()
                            {
                                ProductName = assetInputViewModel.ProductName,
                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                Item = assetInputViewModel.Item,
                                Amount = assetInputViewModel.Amount,
                                MonetaryUnit = assetInputViewModel.MonetaryUnit,
                                Created = DateTime.UtcNow,
                                Updated = DateTime.UtcNow,
                                Note = assetInputViewModel.Note ?? "",
                                Deleted = false
                            }
                            );
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }

                    return Json(new { result = true, message = localizer["The asset has been successfully created."].Value });
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> Asset(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<AssetOutputViewModel> assetOutputViewModels = [];

                #region Assets
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                foreach (Asset item in await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        assetOutputViewModels.Add(new AssetOutputViewModel()
                        {
                            ProductName = item.ProductName,
                            Item = localizer[item.Item.ToString()].Value,
                            Amount = item.Amount,
                            MonetaryUnit = item.MonetaryUnit,
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                            Note = item.Note,
                            Deleted = item.Deleted
                        });
                    }
                    else
                    {
                        if ((item.ProductName?.ToString() ?? "").Contains(wholeSearch) ||
                            localizer[item.Item?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Deleted.ToString() ?? "").Contains(wholeSearch))
                        {
                            assetOutputViewModels.Add(new AssetOutputViewModel()
                            {
                                ProductName = item.ProductName,
                                Item = localizer[item.Item.ToString()].Value,
                                Amount = item.Amount,
                                MonetaryUnit = item.MonetaryUnit,
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                                Note = item.Note,
                                Deleted = item.Deleted
                            });
                        }
                    }
                }
                #endregion

                IQueryable<AssetOutputViewModel> result = assetOutputViewModels.OrderBy(m => m.ProductName).AsQueryable();
                return PartialView("_AssetGrid", result);
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsAssetExists(string productName)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, productName);

                return tempAsset == null
                    ? Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value })
                    : (IActionResult)Json(new { result = true, asset = tempAsset });
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
        {
            try
            {
                _ = ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));

                if (ModelState.IsValid)
                {
                    try
                    {
                        #region Validation of item Value
                        if (assetInputViewModel.Item is not ("FreeDepositAndWithdrawal" or
                            "TrustAsset" or
                            "CashAsset" or
                            "SavingsAsset" or
                            "InvestmentAsset" or
                            "RealEstate" or
                            "Movables" or
                            "OtherPhysicalAsset" or
                            "InsuranceAsset"))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                        Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.OriginalProductName);

                        if (tempAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value });
                        }
                        else
                        {
                            tempAsset.ProductName = assetInputViewModel.ProductName;
                            tempAsset.Item = assetInputViewModel.Item;
                            tempAsset.Amount = assetInputViewModel.Amount;
                            tempAsset.Note = assetInputViewModel.Note;
                            tempAsset.Deleted = assetInputViewModel.Deleted;
                            tempAsset.Updated = DateTime.UtcNow;

                            return await assetRepository.UpdateAssetWithProductNameAsync(tempAsset, assetInputViewModel.OriginalProductName) switch
                            {
                                > 0 => Json(new { result = true, message = localizer["The asset has been successfully updated."].Value }),
                                _ => Json(new { result = false, error = localizer["Input is invalid"].Value }),
                            };
                        }
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.ProductName);

                if (tempAsset == null)
                {
                    return Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value });
                }
                else
                {
                    tempAsset.Deleted = true;
                    await assetRepository.UpdateAssetAsync(tempAsset);
                    return Json(new { result = true, message = localizer["The asset has been successfully deleted."].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> ExportExcelAsset(string fileName = "")
        {
            // query data from database
            await Task.Yield();

            List<AssetOutputViewModel> assetOutputViewModels = [];

            #region Assets

            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

            foreach (Asset item in await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                assetOutputViewModels.Add(new AssetOutputViewModel()
                {
                    ProductName = item.ProductName,
                    Item = item.Item,
                    Amount = item.Amount,
                    MonetaryUnit = item.MonetaryUnit,
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                    Note = item.Note,
                    Deleted = item.Deleted
                });
            }

            assetOutputViewModels = assetOutputViewModels.OrderBy(m => m.ProductName).ToList();

            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(AssetOutputViewModel.ProductName).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(AssetOutputViewModel.Item).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(AssetOutputViewModel.Amount).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(AssetOutputViewModel.MonetaryUnit).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(AssetOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(AssetOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(AssetOutputViewModel.Note).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(AssetOutputViewModel.Deleted).ToString()].Value;

                int recordIndex = 2;
                foreach (AssetOutputViewModel item in assetOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.ProductName;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.Item?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Amount;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.MonetaryUnit;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Note;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Deleted;
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OSTimeZone.DestinationTimeZoneId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion

        #region 수입

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                        #region Validate ProductName's Deletion
                        Asset tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, incomeInputViewModel.DepositMyAssetProductName);
                        if (tempAssetToValidate.Deleted)
                        {
                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                        }
                        #endregion

                        #region Validation of MainClass & SubClass Value
                        if (incomeInputViewModel.MainClass == "RegularIncome")
                        {
                            if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (incomeInputViewModel.MainClass == "IrregularIncome")
                        {
                            if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region 수입 생성
                        await incomeRepository.CreateIncomeAsync
                            (
                            new Income()
                            {
                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = incomeInputViewModel.MainClass,
                                SubClass = incomeInputViewModel.SubClass,
                                Contents = incomeInputViewModel.Contents ?? "",
                                Amount = Math.Abs(incomeInputViewModel.Amount),
                                DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
                                Created = incomeInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = incomeInputViewModel.Note ?? ""
                            }
                            );
                        #endregion

                        #region 자산 업데이트
                        Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, incomeInputViewModel.DepositMyAssetProductName);
                        tempAsset.Amount += Math.Abs(incomeInputViewModel.Amount);
                        tempAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(tempAsset);
                        #endregion

                        return Json(new { result = true, message = localizer["The income has been successfully created."].Value });
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> Income(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<IncomeOutputViewModel> incomeOutputViewModels = [];

                #region Incomes
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                foreach (Income item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        incomeOutputViewModels.Add(new IncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Contents = item.Contents,
                            Amount = item.Amount,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                            Note = item.Note
                        });
                    }
                    else
                    {
                        if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch))
                        {
                            incomeOutputViewModels.Add(new IncomeOutputViewModel()
                            {
                                Id = item.Id,
                                MainClass = localizer[item.MainClass.ToString()].Value,
                                SubClass = localizer[item.SubClass.ToString()].Value,
                                Contents = item.Contents,
                                Amount = item.Amount,
                                DepositMyAssetProductName = item.DepositMyAssetProductName,
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                                Note = item.Note
                            });
                        }
                    }
                }
                #endregion

                IQueryable<IncomeOutputViewModel> result = incomeOutputViewModels.OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
                return PartialView("_IncomeGrid", result);
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsIncomeExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                List<Income> tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempIncomes == null)
                {
                    return Json(new { result = false, error = localizer["No income exists"].Value });
                }
                else
                {
                    Income tempIncome = tempIncomes.Where(a => a.Id == id).FirstOrDefault();

                    if (tempIncome == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        tempIncome.Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(tempIncome.Created, OSTimeZone.DestinationTimeZoneId);
                        return Json(new { result = true, income = tempIncome });
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                        #region Validation of MainClass & SubClass Value
                        if (incomeInputViewModel.MainClass == "RegularIncome")
                        {
                            if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                                "BusinessIncome" or
                                "PensionIncome" or
                                "FinancialIncome" or
                                "RentalIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (incomeInputViewModel.MainClass == "IrregularIncome")
                        {
                            if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                                "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region 수입 & 자산 업데이트
                        List<Income> tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                        if (tempIncomes == null)
                        {
                            return Json(new { result = false, error = localizer["No income exists"].Value });
                        }
                        else
                        {
                            Income tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id).FirstOrDefault();

                            if (tempIncome == null)
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                            else
                            {
                                #region Validate ProductName's Deletion
                                Asset tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
                                if (tempAssetToValidate.Deleted)
                                {
                                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                }
                                #endregion
                                string previousTempIncomeDepositMyAssetProductName = tempIncome.DepositMyAssetProductName;
                                long previousTempIncomeAmount = tempIncome.Amount;

                                #region 수입 업데이트
                                tempIncome.MainClass = incomeInputViewModel.MainClass;
                                tempIncome.SubClass = incomeInputViewModel.SubClass;
                                tempIncome.Contents = incomeInputViewModel.Contents;
                                tempIncome.Amount = incomeInputViewModel.Amount;
                                tempIncome.DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName;
                                tempIncome.Note = incomeInputViewModel.Note;
                                tempIncome.Created = incomeInputViewModel.Created;
                                tempIncome.Updated = DateTime.UtcNow;

                                await incomeRepository.UpdateIncomeAsync(tempIncome);
                                #endregion

                                #region 자산 업데이트
                                if (tempIncome.DepositMyAssetProductName == previousTempIncomeDepositMyAssetProductName) // 같은 경우
                                {
                                    #region 자산 업데이트
                                    Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
                                    tempAsset.Amount = tempAsset.Amount - Math.Abs(previousTempIncomeAmount) + Math.Abs(tempIncome.Amount);
                                    tempAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(tempAsset);
                                    #endregion
                                }
                                else // 다른 경우
                                {
                                    #region 구 자산 업데이트
                                    Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempIncomeDepositMyAssetProductName);
                                    tempAsset.Amount -= Math.Abs(previousTempIncomeAmount);
                                    tempAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(tempAsset);
                                    #endregion

                                    #region 신 자산 업데이트
                                    tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
                                    tempAsset.Amount += Math.Abs(tempIncome.Amount);
                                    tempAsset.Updated = DateTime.UtcNow;
                                    await assetRepository.UpdateAssetAsync(tempAsset);
                                    #endregion
                                }
                                #endregion

                                return Json(new { result = true, message = localizer["The income has been successfully updated."].Value });
                            }
                        }
                        #endregion
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                List<Income> tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempIncomes == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    Income tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id).FirstOrDefault();

                    if (tempIncome == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        #region Validate ProductName's Deletion
                        Asset tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
                        if (tempAssetToValidate.Deleted)
                        {
                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                        }
                        #endregion

                        #region 자산 업데이트
                        Asset tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
                        tempAsset.Amount -= Math.Abs(tempIncome.Amount);
                        tempAsset.Updated = DateTime.UtcNow;
                        await assetRepository.UpdateAssetAsync(tempAsset);
                        #endregion

                        #region 수입 삭제
                        await incomeRepository.DeleteIncomeAsync(tempIncome);
                        #endregion

                        return Json(new { result = true, message = localizer["The income has been successfully deleted."].Value });
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> ExportExcelIncome(string fileName = "")
        {
            // query data from database
            await Task.Yield();

            List<IncomeOutputViewModel> incomeOutputViewModels = [];

            #region Incomes

            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

            foreach (Income item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                incomeOutputViewModels.Add(new IncomeOutputViewModel()
                {
                    MainClass = item.MainClass,
                    SubClass = item.SubClass,
                    Contents = item.Contents,
                    Amount = item.Amount,
                    DepositMyAssetProductName = item.DepositMyAssetProductName,
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                    Note = item.Note
                });
            }
            incomeOutputViewModels = incomeOutputViewModels.OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();

            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(IncomeOutputViewModel.MainClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(IncomeOutputViewModel.SubClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(IncomeOutputViewModel.Contents).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(IncomeOutputViewModel.Amount).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(IncomeOutputViewModel.DepositMyAssetProductName).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(IncomeOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(IncomeOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(IncomeOutputViewModel.Note).ToString()].Value;

                int recordIndex = 2;
                foreach (IncomeOutputViewModel item in incomeOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Contents;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.Amount;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.DepositMyAssetProductName;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Note;
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OSTimeZone.DestinationTimeZoneId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion

        #region 지출

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> CreateExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                    #region Validate ProductName's Deletion
                    Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
                    if (tempExpenditureAssetToValidate.Deleted)
                    {
                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                    }
                    if (!string.IsNullOrEmpty(expenditureInputViewModel.MyDepositAsset))
                    {
                        Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);
                        if (tempIncomeAssetToValidate.Deleted)
                        {
                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                        }
                    }
                    #endregion

                    #region 대분류 = '정기저축'
                    if (expenditureInputViewModel.MainClass == "RegularSavings")
                    {
                        if (expenditureInputViewModel.SubClass is "Deposit" or "Investment")
                        {
                            try
                            {
                                if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                #region 지출 생성
                                await expenditureRepository.CreateExpenditureAsync
                                    (
                                    new Expenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = expenditureInputViewModel.MainClass,
                                        SubClass = expenditureInputViewModel.SubClass,
                                        Contents = expenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(expenditureInputViewModel.Amount),
                                        PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                        Created = expenditureInputViewModel.Created,
                                        Updated = DateTime.UtcNow,
                                        Note = expenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                #region 지출 자산 업데이트
                                expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                                expenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(expenditureAsset);
                                #endregion

                                #region 수입 자산 업데이트
                                incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                                incomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(incomeAsset);
                                #endregion

                                return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
                            }
                            catch (Exception e)
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
                    else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
                    {
                        if (expenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                        {
                            try
                            {
                                if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
                                Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);

                                if (expenditureAsset == null || incomeAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                #region 지출 생성
                                await expenditureRepository.CreateExpenditureAsync
                                    (
                                    new Expenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = expenditureInputViewModel.MainClass,
                                        SubClass = expenditureInputViewModel.SubClass,
                                        Contents = expenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(expenditureInputViewModel.Amount),
                                        PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                        Created = expenditureInputViewModel.Created,
                                        Updated = DateTime.UtcNow,
                                        Note = expenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                #region 지출 자산 업데이트
                                expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                                expenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(expenditureAsset);
                                #endregion

                                #region 수입 자산 업데이트
                                incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                                incomeAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(incomeAsset);
                                #endregion

                                return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
                            }
                            catch (Exception e)
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (expenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer")
                        {
                            try
                            {
                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);

                                if (expenditureAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                #region 지출 생성
                                await expenditureRepository.CreateExpenditureAsync
                                    (
                                    new Expenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = expenditureInputViewModel.MainClass,
                                        SubClass = expenditureInputViewModel.SubClass,
                                        Contents = expenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(expenditureInputViewModel.Amount),
                                        PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        Created = expenditureInputViewModel.Created,
                                        Updated = DateTime.UtcNow,
                                        Note = expenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                #region 지출 자산 업데이트
                                expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                                expenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(expenditureAsset);
                                #endregion

                                return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
                            }
                            catch (Exception e)
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
                    else if (expenditureInputViewModel.MainClass == "ConsumerSpending")
                    {
                        if (expenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
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
                                Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);

                                if (expenditureAsset == null)
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }

                                #region 지출 생성
                                await expenditureRepository.CreateExpenditureAsync
                                    (
                                    new Expenditure()
                                    {
                                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                        MainClass = expenditureInputViewModel.MainClass,
                                        SubClass = expenditureInputViewModel.SubClass,
                                        Contents = expenditureInputViewModel.Contents ?? "",
                                        Amount = Math.Abs(expenditureInputViewModel.Amount),
                                        PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                        MyDepositAsset = "",
                                        Created = expenditureInputViewModel.Created,
                                        Updated = DateTime.UtcNow,
                                        Note = expenditureInputViewModel.Note ?? ""
                                    }
                                    );
                                #endregion

                                #region 지출 자산 업데이트
                                expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                                expenditureAsset.Updated = DateTime.UtcNow;
                                await assetRepository.UpdateAssetAsync(expenditureAsset);
                                #endregion

                                return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
                            }
                            catch (Exception e)
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
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> Expenditure(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<ExpenditureOutputViewModel> expenditureOutputViewModels = [];

                #region Expenditures
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                foreach (Expenditure item in await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = localizer[item.MainClass.ToString()].Value,
                            SubClass = localizer[item.SubClass.ToString()].Value,
                            Contents = item.Contents,
                            Amount = item.Amount,
                            PaymentMethod = item.PaymentMethod,
                            Note = item.Note,
                            MyDepositAsset = item.MyDepositAsset,
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
                        });
                    }
                    else
                    {
                        if (localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                            (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
                            (item.PaymentMethod?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch))
                        {
                            expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                            {
                                Id = item.Id,
                                MainClass = localizer[item.MainClass.ToString()].Value,
                                SubClass = localizer[item.SubClass.ToString()].Value,
                                Contents = item.Contents,
                                Amount = item.Amount,
                                PaymentMethod = item.PaymentMethod,
                                Note = item.Note,
                                MyDepositAsset = item.MyDepositAsset,
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
                            });
                        }
                    }
                }
                #endregion

                IQueryable<ExpenditureOutputViewModel> result = expenditureOutputViewModels.OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
                return PartialView("_ExpenditureGrid", result);
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> IsExpenditureExists(int id)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["No expenditure exists"].Value });
                }
                else
                {
                    Expenditure tempExpenditure = tempExpenditures.Where(a => a.Id == id).FirstOrDefault();

                    if (tempExpenditure == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        tempExpenditure.Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(tempExpenditure.Created, OSTimeZone.DestinationTimeZoneId);
                        return Json(new { result = true, expenditure = tempExpenditure });
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                    #region 대분류 = '정기저축'
                    if (expenditureInputViewModel.MainClass == "RegularSavings")
                    {
                        if (expenditureInputViewModel.SubClass is "Deposit" or "Investment")
                        {
                            try
                            {
                                if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                Expenditure previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Contents = expenditureInputViewModel?.Contents ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel?.Note ?? "",
                                    Asset = previousTempExpenditure.Asset
                                };

                                #region Validate ProductName's Deletion
                                Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                }
                                if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                                {
                                    Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                    }
                                }
                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
                                if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod) // else 보고 참조함.
                                    {
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);
                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                    }
                                    else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset) // else 보고 참조함.
                                    {
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        #region 과거 자산
                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
                                else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                        !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산
                                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산

                                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산
                                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 잘못된 경우
                                else
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }
                                #endregion
                            }
                            catch (Exception e)
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
                    else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
                    {
                        if (expenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                        {
                            try
                            {
                                if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                                {
                                    return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
                                }

                                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                Expenditure previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Contents = expenditureInputViewModel?.Contents ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel?.Note ?? "",
                                    Asset = previousTempExpenditure.Asset
                                };

                                #region Validate ProductName's Deletion
                                Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                }
                                if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                                {
                                    Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                    }
                                }
                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
                                if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion
                                        //#region 자산 업데이트

                                        //#region 과거 자산
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        //#endregion

                                        //#region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        //#endregion

                                        //#endregion
                                    }
                                    else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                                        #endregion

                                        #endregion
                                        //#region 자산 업데이트

                                        //#region 과거 자산
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        //#endregion

                                        //#region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
                                else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                        !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);

                                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산
                                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                    {
                                        if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산

                                            currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            currentIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
                                    {
                                        if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                        else
                                        {
                                            #region 자산 업데이트

                                            #region 과거 자산
                                            Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                            pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
                                            pastIncomeAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            #endregion

                                            #region 현재 자산
                                            currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                                            #endregion

                                            #endregion

                                            //#region 자산 업데이트

                                            //#region 과거 자산
                                            //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                            //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                            //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                            //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                            //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                            //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                            //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                            //pastIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                            //#endregion

                                            //#region 현재 자산
                                            //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                            //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                            //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                            //currentIncomeAsset.Updated = DateTime.UtcNow;
                                            //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                            //#endregion

                                            //#endregion
                                        }
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        Asset currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 잘못된 경우
                                else
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }
                                #endregion
                            }
                            catch (Exception e)
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (expenditureInputViewModel.SubClass is "Tax" or
                            "SocialInsurance" or
                            "InterHouseholdTranserExpenses" or
                            "NonProfitOrganizationTransfer")
                        {
                            try
                            {
                                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                Expenditure previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Contents = expenditureInputViewModel?.Contents ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel?.Note ?? "",
                                    Asset = previousTempExpenditure.Asset
                                };

                                #region Validate ProductName's Deletion
                                Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                }
                                if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                                {
                                    Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                    }
                                }
                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
                                if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
                                else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                        !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 잘못된 경우
                                else
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }
                                #endregion
                            }
                            catch (Exception e)
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
                    else if (expenditureInputViewModel.MainClass == "ConsumerSpending")
                    {
                        if (expenditureInputViewModel.SubClass is "MealOrEatOutExpenses" or
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
                                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                                Expenditure previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                                Expenditure currentTempExpenditure = new()
                                {
                                    Id = expenditureInputViewModel.Id,
                                    AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Contents = expenditureInputViewModel?.Contents ?? "",
                                    Amount = expenditureInputViewModel.Amount,
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel?.Note ?? "",
                                    Asset = previousTempExpenditure.Asset
                                };

                                #region Validate ProductName's Deletion
                                Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                if (tempExpenditureAssetToValidate.Deleted)
                                {
                                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                }
                                if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                                {
                                    Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
                                    if (tempIncomeAssetToValidate.Deleted)
                                    {
                                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                                    }
                                }
                                #endregion

                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
                                if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                    !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
                                else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                        !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                                {
                                    #region 지출 업데이트
                                    await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
                                    #endregion

                                    if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }
                                    else
                                    {
                                        #region 자산 업데이트

                                        #region 과거 자산
                                        Asset pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        Asset pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        Asset currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(pastIncomeAsset);
                                        #endregion

                                        #region 현재 자산
                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
                                        #endregion

                                        #endregion
                                    }

                                    return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
                                }
                                #endregion
                                #region 잘못된 경우
                                else
                                {
                                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                                }
                                #endregion
                            }
                            catch (Exception e)
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
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

                List<Expenditure> tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                if (tempExpenditures == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
                else
                {
                    Expenditure tempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                    if (tempExpenditure == null)
                    {
                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        #region Validate ProductName's Deletion
                        Asset tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);
                        if (tempExpenditureAssetToValidate.Deleted)
                        {
                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                        }
                        if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
                        {
                            Asset tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.MyDepositAsset);
                            if (tempIncomeAssetToValidate.Deleted)
                            {
                                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                            }
                        }
                        #endregion

                        if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
                        {
                            #region 지출 삭제
                            await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);
                            #endregion

                            #region 자산 업데이트
                            Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);
                            Asset incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.MyDepositAsset);

                            expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(expenditureAsset);

                            incomeAsset.Amount -= Math.Abs(tempExpenditure.Amount);
                            incomeAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(incomeAsset);
                            #endregion

                            return Json(new { result = true, message = localizer["The expenditure has been successfully deleted."].Value });
                        }
                        else
                        {
                            #region 지출 삭제
                            await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);
                            #endregion

                            #region 자산 업데이트
                            Asset expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);

                            expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await assetRepository.UpdateAssetAsync(expenditureAsset);
                            #endregion

                            return Json(new { result = true, message = localizer["The expenditure has been successfully deleted."].Value });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }
        }
        #endregion

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
        {
            // query data from database
            await Task.Yield();

            List<ExpenditureOutputViewModel> expenditureOutputViewModels = [];

            #region Expenditures

            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);

            foreach (Expenditure item in await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                {
                    MainClass = localizer[item.MainClass.ToString()].Value,
                    SubClass = localizer[item.SubClass.ToString()].Value,
                    Contents = item.Contents,
                    Amount = item.Amount,
                    PaymentMethod = item.PaymentMethod,
                    Note = item.Note,
                    MyDepositAsset = item.MyDepositAsset,
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
                });
            }
            expenditureOutputViewModels = expenditureOutputViewModels.OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();

            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(ExpenditureOutputViewModel.MainClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(ExpenditureOutputViewModel.SubClass).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(ExpenditureOutputViewModel.Contents).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(ExpenditureOutputViewModel.Amount).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(ExpenditureOutputViewModel.PaymentMethod).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(ExpenditureOutputViewModel.Note).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(ExpenditureOutputViewModel.MyDepositAsset).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(ExpenditureOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(ExpenditureOutputViewModel.Updated).ToString()].Value;

                int recordIndex = 2;
                foreach (ExpenditureOutputViewModel item in expenditureOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Contents;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.Amount;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.PaymentMethod;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Note;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.MyDepositAsset;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.Updated.ToString();

                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OSTimeZone.DestinationTimeZoneId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion
    }
}