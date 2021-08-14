using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.Site.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Services;
using Microsoft.AspNetCore.Authorization;
using MyLaboratory.Site.Common;
using System.Linq;
using NonFactors.Mvc.Grid;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using OfficeOpenXml;
using MyLaboratory.Site.Contracts;
using MyLaboratory.Site.Models.ViewModels.Management;
using MyLaboratory.Site.Helpers;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc.Localization;
using MyLaboratory.Site.Models.ViewModels.AccountBook;
using MyLaboratory.Site.Models.ViewModels.Notice;

namespace MyLaboratory.Site.Controllers
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
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        #region Validate ProductName's Deletion
                        var tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, fixedIncomeInputViewModel.DepositMyAssetProductName);
                        if (tempAssetToValidate.Deleted)
                        {
                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
                        }
                        #endregion

                        #region Validation of MainClass & SubClass Value
                        if (fixedIncomeInputViewModel.MainClass == "RegularIncome")
                        {
                            if (!(fixedIncomeInputViewModel.SubClass == "LaborIncome" ||
                                fixedIncomeInputViewModel.SubClass == "BusinessIncome" ||
                                fixedIncomeInputViewModel.SubClass == "PensionIncome" ||
                                fixedIncomeInputViewModel.SubClass == "FinancialIncome" ||
                                fixedIncomeInputViewModel.SubClass == "RentalIncome" ||
                                fixedIncomeInputViewModel.SubClass == "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.MainClass == "IrregularIncome")
                        {
                            if (!(fixedIncomeInputViewModel.SubClass == "LaborIncome" ||
                                fixedIncomeInputViewModel.SubClass == "OtherIncome"))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        #endregion

                        #region Validate of DepositMonth
                        if (!(fixedIncomeInputViewModel.DepositMonth >= 1 && fixedIncomeInputViewModel.DepositMonth <= 12))
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }
                        #endregion

                        #region Validate of DepositDay
                        if (fixedIncomeInputViewModel.DepositMonth == 1 ||
                            fixedIncomeInputViewModel.DepositMonth == 3 ||
                            fixedIncomeInputViewModel.DepositMonth == 5 ||
                            fixedIncomeInputViewModel.DepositMonth == 7 ||
                            fixedIncomeInputViewModel.DepositMonth == 8 ||
                            fixedIncomeInputViewModel.DepositMonth == 10 ||
                            fixedIncomeInputViewModel.DepositMonth == 12)
                        {
                            if (!(fixedIncomeInputViewModel.DepositDay >= 1 && fixedIncomeInputViewModel.DepositDay <= 31))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth == 2)
                        {
                            if (!(fixedIncomeInputViewModel.DepositDay >= 1 && fixedIncomeInputViewModel.DepositDay <= 29))
                            {
                                return Json(new { result = false, error = localizer["Input is invalid"].Value });
                            }
                        }
                        else if (fixedIncomeInputViewModel.DepositMonth == 4 ||
                                fixedIncomeInputViewModel.DepositMonth == 6 ||
                                fixedIncomeInputViewModel.DepositMonth == 9 ||
                                fixedIncomeInputViewModel.DepositMonth == 11)
                        {
                            if (!(fixedIncomeInputViewModel.DepositDay >= 1 && fixedIncomeInputViewModel.DepositDay <= 30))
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

                        return Json(new { result = true, message = localizer["The FixedIncome has been successfully created."].Value });
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
                    }
                }
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public IActionResult FixedIncome()
        {
            return View();
        }
        #endregion

        #endregion

        #region 고정 지출

        #region Read
        [HttpGet]
        public IActionResult FixedExpenditure()
        {
            return View();
        }
        #endregion

        #endregion

        //#region 수입

        //#region Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            try
        //            {
        //                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //                #region Validate ProductName's Deletion
        //                var tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, incomeInputViewModel.DepositMyAssetProductName);
        //                if (tempAssetToValidate.Deleted)
        //                {
        //                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                }
        //                #endregion

        //                #region Validation of MainClass & SubClass Value
        //                if (incomeInputViewModel.MainClass == "RegularIncome")
        //                {
        //                    if (!(incomeInputViewModel.SubClass == "LaborIncome" ||
        //                        incomeInputViewModel.SubClass == "BusinessIncome" ||
        //                        incomeInputViewModel.SubClass == "PensionIncome" ||
        //                        incomeInputViewModel.SubClass == "FinancialIncome" ||
        //                        incomeInputViewModel.SubClass == "RentalIncome" ||
        //                        incomeInputViewModel.SubClass == "OtherIncome"))
        //                    {
        //                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                    }
        //                }
        //                else if (incomeInputViewModel.MainClass == "IrregularIncome")
        //                {
        //                    if (!(incomeInputViewModel.SubClass == "LaborIncome" ||
        //                        incomeInputViewModel.SubClass == "OtherIncome"))
        //                    {
        //                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                    }
        //                }
        //                #endregion

        //                #region 수입 생성
        //                await incomeRepository.CreateIncomeAsync
        //                    (
        //                    new Income()
        //                    {
        //                        AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                        MainClass = incomeInputViewModel.MainClass,
        //                        SubClass = incomeInputViewModel.SubClass,
        //                        Contents = incomeInputViewModel.Contents ?? "",
        //                        Amount = Math.Abs(incomeInputViewModel.Amount),
        //                        DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
        //                        Created = DateTime.UtcNow,
        //                        Updated = DateTime.UtcNow,
        //                        Note = incomeInputViewModel.Note ?? ""
        //                    }
        //                    );
        //                #endregion

        //                #region 자산 업데이트
        //                var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, incomeInputViewModel.DepositMyAssetProductName);
        //                tempAsset.Amount += Math.Abs(incomeInputViewModel.Amount);
        //                tempAsset.Updated = DateTime.UtcNow;
        //                await assetRepository.UpdateAssetAsync(tempAsset);
        //                #endregion

        //                return Json(new { result = true, message = localizer["The income has been successfully created."].Value });
        //            }
        //            catch (Exception e)
        //            {
        //                return Json(new { result = false, error = e.ToString() });
        //            }
        //        }
        //        else
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Read
        //[HttpGet]
        //public async Task<IActionResult> Income(string wholeSearch)
        //{
        //    if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        //    {
        //        List<IncomeOutputViewModel> incomeOutputViewModels = new List<IncomeOutputViewModel>();

        //        #region Incomes
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        foreach (var item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
        //        {
        //            if (string.IsNullOrEmpty(wholeSearch))
        //            {
        //                incomeOutputViewModels.Add(new IncomeOutputViewModel()
        //                {
        //                    Id = item.Id,
        //                    MainClass = localizer[item.MainClass.ToString()].Value,
        //                    SubClass = localizer[item.SubClass.ToString()].Value,
        //                    Contents = item.Contents,
        //                    Amount = item.Amount,
        //                    DepositMyAssetProductName = item.DepositMyAssetProductName,
        //                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
        //                    Note = item.Note
        //                });
        //            }
        //            else
        //            {
        //                if ((item.Id.ToString() ?? "").Contains(wholeSearch) ||
        //                    localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
        //                    localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
        //                    (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
        //                    (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.Note?.ToString() ?? "").Contains(wholeSearch))
        //                {
        //                    incomeOutputViewModels.Add(new IncomeOutputViewModel()
        //                    {
        //                        Id = item.Id,
        //                        MainClass = localizer[item.MainClass.ToString()].Value,
        //                        SubClass = localizer[item.SubClass.ToString()].Value,
        //                        Contents = item.Contents,
        //                        Amount = item.Amount,
        //                        DepositMyAssetProductName = item.DepositMyAssetProductName,
        //                        Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //                        Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
        //                        Note = item.Note
        //                    });
        //                }
        //            }
        //        }
        //        #endregion

        //        var result = incomeOutputViewModels.AsQueryable();
        //        return PartialView("_IncomeGrid", result);
        //    }
        //    return View();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> IsIncomeExists(int id)
        //{
        //    try
        //    {
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

        //        if (tempIncomes == null)
        //        {
        //            return Json(new { result = false, error = localizer["No income exists"].Value });
        //        }
        //        else
        //        {
        //            var tempIncome = tempIncomes.Where(a => a.Id == id).FirstOrDefault();

        //            if (tempIncome == null)
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            else
        //            {
        //                return Json(new { result = true, income = tempIncome });
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Update
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            try
        //            {
        //                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //                #region Validation of MainClass & SubClass Value
        //                if (incomeInputViewModel.MainClass == "RegularIncome")
        //                {
        //                    if (!(incomeInputViewModel.SubClass == "LaborIncome" ||
        //                        incomeInputViewModel.SubClass == "BusinessIncome" ||
        //                        incomeInputViewModel.SubClass == "PensionIncome" ||
        //                        incomeInputViewModel.SubClass == "FinancialIncome" ||
        //                        incomeInputViewModel.SubClass == "RentalIncome" ||
        //                        incomeInputViewModel.SubClass == "OtherIncome"))
        //                    {
        //                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                    }
        //                }
        //                else if (incomeInputViewModel.MainClass == "IrregularIncome")
        //                {
        //                    if (!(incomeInputViewModel.SubClass == "LaborIncome" ||
        //                        incomeInputViewModel.SubClass == "OtherIncome"))
        //                    {
        //                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                    }
        //                }
        //                #endregion

        //                #region 수입 & 자산 업데이트
        //                var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

        //                if (tempIncomes == null)
        //                {
        //                    return Json(new { result = false, error = localizer["No income exists"].Value });
        //                }
        //                else
        //                {
        //                    var tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id).FirstOrDefault();

        //                    if (tempIncome == null)
        //                    {
        //                        return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                    }
        //                    else
        //                    {
        //                        #region Validate ProductName's Deletion
        //                        var tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
        //                        if (tempAssetToValidate.Deleted)
        //                        {
        //                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                        }
        //                        #endregion
        //                        var previousTempIncomeDepositMyAssetProductName = tempIncome.DepositMyAssetProductName;
        //                        var previousTempIncomeAmount = tempIncome.Amount;

        //                        #region 수입 업데이트
        //                        tempIncome.MainClass = incomeInputViewModel.MainClass;
        //                        tempIncome.SubClass = incomeInputViewModel.SubClass;
        //                        tempIncome.Contents = incomeInputViewModel.Contents;
        //                        tempIncome.Amount = incomeInputViewModel.Amount;
        //                        tempIncome.DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName;
        //                        tempIncome.Note = incomeInputViewModel.Note;
        //                        tempIncome.Updated = DateTime.UtcNow;

        //                        await incomeRepository.UpdateIncomeAsync(tempIncome);
        //                        #endregion

        //                        #region 자산 업데이트
        //                        if (tempIncome.DepositMyAssetProductName == previousTempIncomeDepositMyAssetProductName) // 같은 경우
        //                        {
        //                            #region 자산 업데이트
        //                            var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
        //                            tempAsset.Amount = tempAsset.Amount - Math.Abs(previousTempIncomeAmount) + Math.Abs(tempIncome.Amount);
        //                            tempAsset.Updated = DateTime.UtcNow;
        //                            await assetRepository.UpdateAssetAsync(tempAsset);
        //                            #endregion
        //                        }
        //                        else // 다른 경우
        //                        {
        //                            #region 구 자산 업데이트
        //                            var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempIncomeDepositMyAssetProductName);
        //                            tempAsset.Amount -= Math.Abs(previousTempIncomeAmount);
        //                            tempAsset.Updated = DateTime.UtcNow;
        //                            await assetRepository.UpdateAssetAsync(tempAsset);
        //                            #endregion

        //                            #region 신 자산 업데이트
        //                            tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
        //                            tempAsset.Amount += Math.Abs(tempIncome.Amount);
        //                            tempAsset.Updated = DateTime.UtcNow;
        //                            await assetRepository.UpdateAssetAsync(tempAsset);
        //                            #endregion
        //                        }
        //                        #endregion

        //                        return Json(new { result = true, message = localizer["The income has been successfully updated."].Value });
        //                    }
        //                }
        //                #endregion
        //            }
        //            catch (Exception e)
        //            {
        //                return Json(new { result = false, error = e.ToString() });
        //            }
        //        }
        //        else
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Delete
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
        //{
        //    try
        //    {
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        var tempIncomes = await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

        //        if (tempIncomes == null)
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //        else
        //        {
        //            var tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id).FirstOrDefault();

        //            if (tempIncome == null)
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            else
        //            {
        //                #region Validate ProductName's Deletion
        //                var tempAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
        //                if (tempAssetToValidate.Deleted)
        //                {
        //                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                }
        //                #endregion

        //                #region 자산 업데이트
        //                var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempIncome.DepositMyAssetProductName);
        //                tempAsset.Amount -= Math.Abs(tempIncome.Amount);
        //                tempAsset.Updated = DateTime.UtcNow;
        //                await assetRepository.UpdateAssetAsync(tempAsset);
        //                #endregion

        //                #region 수입 삭제
        //                await incomeRepository.DeleteIncomeAsync(tempIncome);
        //                #endregion

        //                return Json(new { result = true, message = localizer["The income has been successfully deleted."].Value });
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Excel
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> ExportExcelIncome(string fileName = "")
        //{
        //    // query data from database
        //    await Task.Yield();

        //    List<IncomeOutputViewModel> incomeOutputViewModels = new List<IncomeOutputViewModel>();

        //    #region Incomes

        //    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //    foreach (var item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
        //    {
        //        incomeOutputViewModels.Add(new IncomeOutputViewModel()
        //        {
        //            Id = item.Id,
        //            MainClass = item.MainClass,
        //            SubClass = item.SubClass,
        //            Contents = item.Contents,
        //            Amount = item.Amount,
        //            DepositMyAssetProductName = item.DepositMyAssetProductName,
        //            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
        //            Note = item.Note
        //        });
        //    }
        //    #endregion

        //    var stream = new MemoryStream();

        //    using (var package = new ExcelPackage(stream))
        //    {
        //        var workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

        //        workSheetMenuOutputViewModels.Row(1).Height = 20;
        //        workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        //        workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
        //        workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(IncomeOutputViewModel.Id).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(IncomeOutputViewModel.MainClass).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(IncomeOutputViewModel.SubClass).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(IncomeOutputViewModel.Contents).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(IncomeOutputViewModel.Amount).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(IncomeOutputViewModel.DepositMyAssetProductName).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(IncomeOutputViewModel.Created).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(IncomeOutputViewModel.Updated).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(IncomeOutputViewModel.Note).ToString()].Value;

        //        int recordIndex = 2;
        //        foreach (var item in incomeOutputViewModels)
        //        {
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.Id;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.Contents;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Amount;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.DepositMyAssetProductName;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Created.ToString();
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Updated.ToString();
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.Note;
        //            recordIndex++;
        //        }

        //        package.Save();
        //    }
        //    stream.Position = 0;
        //    string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OSTimeZone.DestinationTimeZoneId).ToString("yyyy-MM-dd-HH-mm-ss-fff")}.xlsx";

        //    return File(stream, "application/octet-stream", excelName);
        //}
        //#endregion

        //#endregion

        //#region 지출

        //#region Create
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> CreateExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //            #region Validate ProductName's Deletion
        //            var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
        //            if (tempExpenditureAssetToValidate.Deleted)
        //            {
        //                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //            }
        //            if (!string.IsNullOrEmpty(expenditureInputViewModel.MyDepositAsset))
        //            {
        //                var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);
        //                if (tempIncomeAssetToValidate.Deleted)
        //                {
        //                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류 = '정기저축'
        //            if (expenditureInputViewModel.MainClass == "RegularSavings")
        //            {
        //                if (expenditureInputViewModel.SubClass == "Deposit" || expenditureInputViewModel.SubClass == "MyAssetTransfer" || expenditureInputViewModel.SubClass == "Investment")
        //                {
        //                    try
        //                    {
        //                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
        //                        {
        //                            return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
        //                        }

        //                        var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
        //                        var incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);

        //                        if (expenditureAsset == null || incomeAsset == null)
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }

        //                        #region 지출 생성
        //                        await expenditureRepository.CreateExpenditureAsync
        //                            (
        //                            new Expenditure()
        //                            {
        //                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                                MainClass = expenditureInputViewModel.MainClass,
        //                                SubClass = expenditureInputViewModel.SubClass,
        //                                Contents = expenditureInputViewModel.Contents ?? "",
        //                                Amount = Math.Abs(expenditureInputViewModel.Amount),
        //                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                                MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
        //                                Created = DateTime.UtcNow,
        //                                Updated = DateTime.UtcNow,
        //                                Note = expenditureInputViewModel.Note ?? ""
        //                            }
        //                            );
        //                        #endregion

        //                        #region 지출 자산 업데이트
        //                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
        //                        expenditureAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(expenditureAsset);
        //                        #endregion

        //                        #region 수입 자산 업데이트
        //                        incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
        //                        incomeAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(incomeAsset);
        //                        #endregion

        //                        return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류 = '비소비지출'
        //            else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
        //            {
        //                if (expenditureInputViewModel.SubClass == "PublicPension" || expenditureInputViewModel.SubClass == "DebtRepayment")
        //                {
        //                    try
        //                    {
        //                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
        //                        {
        //                            return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
        //                        }

        //                        var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);
        //                        var incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.MyDepositAsset);

        //                        if (expenditureAsset == null || incomeAsset == null)
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }

        //                        #region 지출 생성
        //                        await expenditureRepository.CreateExpenditureAsync
        //                            (
        //                            new Expenditure()
        //                            {
        //                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                                MainClass = expenditureInputViewModel.MainClass,
        //                                SubClass = expenditureInputViewModel.SubClass,
        //                                Contents = expenditureInputViewModel.Contents ?? "",
        //                                Amount = Math.Abs(expenditureInputViewModel.Amount),
        //                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                                MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
        //                                Created = DateTime.UtcNow,
        //                                Updated = DateTime.UtcNow,
        //                                Note = expenditureInputViewModel.Note ?? ""
        //                            }
        //                            );
        //                        #endregion

        //                        #region 지출 자산 업데이트
        //                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
        //                        expenditureAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(expenditureAsset);
        //                        #endregion

        //                        #region 수입 자산 업데이트
        //                        incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
        //                        incomeAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(incomeAsset);
        //                        #endregion

        //                        return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else if (expenditureInputViewModel.SubClass == "Tax" ||
        //                    expenditureInputViewModel.SubClass == "SocialInsurance" ||
        //                    expenditureInputViewModel.SubClass == "InterHouseholdTranserExpenses" ||
        //                    expenditureInputViewModel.SubClass == "NonProfitOrganizationTransfer")
        //                {
        //                    try
        //                    {
        //                        var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);

        //                        if (expenditureAsset == null)
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }

        //                        #region 지출 생성
        //                        await expenditureRepository.CreateExpenditureAsync
        //                            (
        //                            new Expenditure()
        //                            {
        //                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                                MainClass = expenditureInputViewModel.MainClass,
        //                                SubClass = expenditureInputViewModel.SubClass,
        //                                Contents = expenditureInputViewModel.Contents ?? "",
        //                                Amount = Math.Abs(expenditureInputViewModel.Amount),
        //                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                                MyDepositAsset = "",
        //                                Created = DateTime.UtcNow,
        //                                Updated = DateTime.UtcNow,
        //                                Note = expenditureInputViewModel.Note ?? ""
        //                            }
        //                            );
        //                        #endregion

        //                        #region 지출 자산 업데이트
        //                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
        //                        expenditureAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(expenditureAsset);
        //                        #endregion

        //                        return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류 = '소비지출'
        //            else if (expenditureInputViewModel.MainClass == "ConsumerSpending")
        //            {
        //                if (expenditureInputViewModel.SubClass == "MealOrEatOutExpenses" ||
        //                    expenditureInputViewModel.SubClass == "HousingOrSuppliesCost" ||
        //                    expenditureInputViewModel.SubClass == "EducationExpenses" ||
        //                    expenditureInputViewModel.SubClass == "MedicalExpenses" ||
        //                    expenditureInputViewModel.SubClass == "TransportationCost" ||
        //                    expenditureInputViewModel.SubClass == "CommunicationCost" ||
        //                    expenditureInputViewModel.SubClass == "LeisureOrCulture" ||
        //                    expenditureInputViewModel.SubClass == "ClothingOrShoes" ||
        //                    expenditureInputViewModel.SubClass == "PinMoney" ||
        //                    expenditureInputViewModel.SubClass == "ProtectionTypeInsurance" ||
        //                    expenditureInputViewModel.SubClass == "OtherExpenses" ||
        //                    expenditureInputViewModel.SubClass == "UnknownExpenditure")
        //                {
        //                    try
        //                    {
        //                        var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, expenditureInputViewModel.PaymentMethod);

        //                        if (expenditureAsset == null)
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }

        //                        #region 지출 생성
        //                        await expenditureRepository.CreateExpenditureAsync
        //                            (
        //                            new Expenditure()
        //                            {
        //                                AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                                MainClass = expenditureInputViewModel.MainClass,
        //                                SubClass = expenditureInputViewModel.SubClass,
        //                                Contents = expenditureInputViewModel.Contents ?? "",
        //                                Amount = Math.Abs(expenditureInputViewModel.Amount),
        //                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                                MyDepositAsset = "",
        //                                Created = DateTime.UtcNow,
        //                                Updated = DateTime.UtcNow,
        //                                Note = expenditureInputViewModel.Note ?? ""
        //                            }
        //                            );
        //                        #endregion

        //                        #region 지출 자산 업데이트
        //                        expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
        //                        expenditureAsset.Updated = DateTime.UtcNow;
        //                        await assetRepository.UpdateAssetAsync(expenditureAsset);
        //                        #endregion

        //                        return Json(new { result = true, message = localizer["The expenditure has been successfully created."].Value });
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류에 없는 값
        //            else
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            #endregion
        //        }
        //        else
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Read
        //[HttpGet]
        //public async Task<IActionResult> Expenditure(string wholeSearch)
        //{
        //    if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        //    {
        //        List<ExpenditureOutputViewModel> expenditureOutputViewModels = new List<ExpenditureOutputViewModel>();

        //        #region Expenditures
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        foreach (var item in await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
        //        {
        //            if (string.IsNullOrEmpty(wholeSearch))
        //            {
        //                expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
        //                {
        //                    Id = item.Id,
        //                    MainClass = localizer[item.MainClass.ToString()].Value,
        //                    SubClass = localizer[item.SubClass.ToString()].Value,
        //                    Contents = item.Contents,
        //                    Amount = item.Amount,
        //                    PaymentMethod = item.PaymentMethod,
        //                    Note = item.Note,
        //                    MyDepositAsset = item.MyDepositAsset,
        //                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
        //                });
        //            }
        //            else
        //            {
        //                if ((item.Id.ToString() ?? "").Contains(wholeSearch) ||
        //                    localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
        //                    localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
        //                    (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.PaymentMethod?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
        //                    (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
        //                    (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch))
        //                {
        //                    expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
        //                    {
        //                        Id = item.Id,
        //                        MainClass = localizer[item.MainClass.ToString()].Value,
        //                        SubClass = localizer[item.SubClass.ToString()].Value,
        //                        Contents = item.Contents,
        //                        Amount = item.Amount,
        //                        PaymentMethod = item.PaymentMethod,
        //                        Note = item.Note,
        //                        MyDepositAsset = item.MyDepositAsset,
        //                        Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //                        Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
        //                    });
        //                }
        //            }
        //        }
        //        #endregion

        //        var result = expenditureOutputViewModels.AsQueryable();
        //        return PartialView("_ExpenditureGrid", result);
        //    }
        //    return View();
        //}
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> IsExpenditureExists(int id)
        //{
        //    try
        //    {
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

        //        if (tempExpenditures == null)
        //        {
        //            return Json(new { result = false, error = localizer["No expenditure exists"].Value });
        //        }
        //        else
        //        {
        //            var tempExpenditure = tempExpenditures.Where(a => a.Id == id).FirstOrDefault();

        //            if (tempExpenditure == null)
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            else
        //            {
        //                return Json(new { result = true, expenditure = tempExpenditure });
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Update
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> UpdateExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //            expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);

        //            #region 대분류 = '정기저축'
        //            if (expenditureInputViewModel.MainClass == "RegularSavings")
        //            {
        //                if (expenditureInputViewModel.SubClass == "Deposit" || expenditureInputViewModel.SubClass == "MyAssetTransfer" || expenditureInputViewModel.SubClass == "Investment")
        //                {
        //                    try
        //                    {
        //                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
        //                        {
        //                            return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
        //                        }

        //                        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
        //                        var previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
        //                        var currentTempExpenditure = new Expenditure()
        //                        {
        //                            Id = expenditureInputViewModel.Id,
        //                            AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                            MainClass = expenditureInputViewModel.MainClass,
        //                            SubClass = expenditureInputViewModel.SubClass,
        //                            Contents = expenditureInputViewModel?.Contents ?? "",
        //                            Amount = expenditureInputViewModel.Amount,
        //                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                            MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
        //                            Created = previousTempExpenditure.Created,
        //                            Updated = DateTime.UtcNow,
        //                            Note = expenditureInputViewModel?.Note ?? "",
        //                            Asset = previousTempExpenditure.Asset
        //                        };

        //                        #region Validate ProductName's Deletion
        //                        var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                        if (tempExpenditureAssetToValidate.Deleted)
        //                        {
        //                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                        }
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
        //                        {
        //                            var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
        //                            if (tempIncomeAssetToValidate.Deleted)
        //                            {
        //                                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                            }
        //                        }
        //                        #endregion

        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                            !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod) // else 보고 참조함.
        //                            {
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);
        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                            }
        //                            else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset) // else 보고 참조함.
        //                            {
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                #region 과거 자산
        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
        //                        else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

        //                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
        //                            {
        //                                if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산
        //                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산

        //                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
        //                            {
        //                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산
        //                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 잘못된 경우
        //                        else
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }
        //                        #endregion
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류 = '비소비지출'
        //            else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
        //            {
        //                if (expenditureInputViewModel.SubClass == "PublicPension" || expenditureInputViewModel.SubClass == "DebtRepayment")
        //                {
        //                    try
        //                    {
        //                        if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
        //                        {
        //                            return Json(new { result = false, error = localizer["The PaymentMethod and MyDepositAsset value cannot be the same."].Value });
        //                        }

        //                        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
        //                        var previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
        //                        var currentTempExpenditure = new Expenditure()
        //                        {
        //                            Id = expenditureInputViewModel.Id,
        //                            AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                            MainClass = expenditureInputViewModel.MainClass,
        //                            SubClass = expenditureInputViewModel.SubClass,
        //                            Contents = expenditureInputViewModel?.Contents ?? "",
        //                            Amount = expenditureInputViewModel.Amount,
        //                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                            MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
        //                            Created = previousTempExpenditure.Created,
        //                            Updated = DateTime.UtcNow,
        //                            Note = expenditureInputViewModel?.Note ?? "",
        //                            Asset = previousTempExpenditure.Asset
        //                        };

        //                        #region Validate ProductName's Deletion
        //                        var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                        if (tempExpenditureAssetToValidate.Deleted)
        //                        {
        //                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                        }
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
        //                        {
        //                            var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
        //                            if (tempIncomeAssetToValidate.Deleted)
        //                            {
        //                                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                            }
        //                        }
        //                        #endregion

        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                            !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion
        //                                //#region 자산 업데이트

        //                                //#region 과거 자산
        //                                //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                //#endregion

        //                                //#region 현재 자산
        //                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                //#endregion

        //                                //#endregion
        //                            }
        //                            else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
        //                                #endregion

        //                                #endregion
        //                                //#region 자산 업데이트

        //                                //#region 과거 자산
        //                                //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                //#endregion

        //                                //#region 현재 자산
        //                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                //#endregion

        //                                //#endregion
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(O))]
        //                        else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);

        //                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
        //                            {
        //                                if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                                {
        //                                    #region 자산 업데이트
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산
        //                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.MyDepositAsset)
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산

        //                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.MyDepositAsset)
        //                            {
        //                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                                else
        //                                {
        //                                    #region 자산 업데이트

        //                                    #region 과거 자산
        //                                    var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) + Math.Abs(currentTempExpenditure.Amount);
        //                                    pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    #endregion

        //                                    #region 현재 자산
        //                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
        //                                    #endregion

        //                                    #endregion

        //                                    //#region 자산 업데이트

        //                                    //#region 과거 자산
        //                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                    //#endregion

        //                                    //#region 현재 자산
        //                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                    //#endregion

        //                                    //#endregion
        //                                }
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 잘못된 경우
        //                        else
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }
        //                        #endregion
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else if (expenditureInputViewModel.SubClass == "Tax" ||
        //                    expenditureInputViewModel.SubClass == "SocialInsurance" ||
        //                    expenditureInputViewModel.SubClass == "InterHouseholdTranserExpenses" ||
        //                    expenditureInputViewModel.SubClass == "NonProfitOrganizationTransfer")
        //                {
        //                    try
        //                    {
        //                        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
        //                        var previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
        //                        var currentTempExpenditure = new Expenditure()
        //                        {
        //                            Id = expenditureInputViewModel.Id,
        //                            AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                            MainClass = expenditureInputViewModel.MainClass,
        //                            SubClass = expenditureInputViewModel.SubClass,
        //                            Contents = expenditureInputViewModel?.Contents ?? "",
        //                            Amount = expenditureInputViewModel.Amount,
        //                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                            MyDepositAsset = "",
        //                            Created = previousTempExpenditure.Created,
        //                            Updated = DateTime.UtcNow,
        //                            Note = expenditureInputViewModel?.Note ?? "",
        //                            Asset = previousTempExpenditure.Asset
        //                        };

        //                        #region Validate ProductName's Deletion
        //                        var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                        if (tempExpenditureAssetToValidate.Deleted)
        //                        {
        //                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                        }
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
        //                        {
        //                            var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
        //                            if (tempIncomeAssetToValidate.Deleted)
        //                            {
        //                                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                            }
        //                        }
        //                        #endregion

        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                            !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
        //                        else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 잘못된 경우
        //                        else
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }
        //                        #endregion
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류 = '소비지출'
        //            else if (expenditureInputViewModel.MainClass == "ConsumerSpending")
        //            {
        //                if (expenditureInputViewModel.SubClass == "MealOrEatOutExpenses" ||
        //                    expenditureInputViewModel.SubClass == "HousingOrSuppliesCost" ||
        //                    expenditureInputViewModel.SubClass == "EducationExpenses" ||
        //                    expenditureInputViewModel.SubClass == "MedicalExpenses" ||
        //                    expenditureInputViewModel.SubClass == "TransportationCost" ||
        //                    expenditureInputViewModel.SubClass == "CommunicationCost" ||
        //                    expenditureInputViewModel.SubClass == "LeisureOrCulture" ||
        //                    expenditureInputViewModel.SubClass == "ClothingOrShoes" ||
        //                    expenditureInputViewModel.SubClass == "PinMoney" ||
        //                    expenditureInputViewModel.SubClass == "ProtectionTypeInsurance" ||
        //                    expenditureInputViewModel.SubClass == "OtherExpenses" ||
        //                    expenditureInputViewModel.SubClass == "UnknownExpenditure")
        //                {
        //                    try
        //                    {
        //                        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
        //                        var previousTempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
        //                        var currentTempExpenditure = new Expenditure()
        //                        {
        //                            Id = expenditureInputViewModel.Id,
        //                            AccountEmail = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email,
        //                            MainClass = expenditureInputViewModel.MainClass,
        //                            SubClass = expenditureInputViewModel.SubClass,
        //                            Contents = expenditureInputViewModel?.Contents ?? "",
        //                            Amount = expenditureInputViewModel.Amount,
        //                            PaymentMethod = expenditureInputViewModel.PaymentMethod,
        //                            MyDepositAsset = "",
        //                            Created = previousTempExpenditure.Created,
        //                            Updated = DateTime.UtcNow,
        //                            Note = expenditureInputViewModel?.Note ?? "",
        //                            Asset = previousTempExpenditure.Asset
        //                        };

        //                        #region Validate ProductName's Deletion
        //                        var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                        if (tempExpenditureAssetToValidate.Deleted)
        //                        {
        //                            return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                        }
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
        //                        {
        //                            var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);
        //                            if (tempIncomeAssetToValidate.Deleted)
        //                            {
        //                                return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                            }
        //                        }
        //                        #endregion

        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(X))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
        //                        if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                            !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                //pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 과거 지출 값 [(결제수단(O)), (내입금자산(O))], 현재 지출 값 [(결제수단(O)), (내입금자산(X))]
        //                        else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) && !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
        //                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) && string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
        //                        {
        //                            #region 지출 업데이트
        //                            await expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure);
        //                            #endregion

        //                            if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount = pastExpenditureAsset.Amount + Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else if (previousTempExpenditure.MyDepositAsset == currentTempExpenditure.PaymentMethod)
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount = pastIncomeAsset.Amount - Math.Abs(previousTempExpenditure.Amount) - Math.Abs(currentTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                //currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }
        //                            else
        //                            {
        //                                #region 자산 업데이트

        //                                #region 과거 자산
        //                                var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
        //                                var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

        //                                var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
        //                                //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

        //                                pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
        //                                pastExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastExpenditureAsset);

        //                                pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
        //                                pastIncomeAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(pastIncomeAsset);
        //                                #endregion

        //                                #region 현재 자산
        //                                currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
        //                                currentExpenditureAsset.Updated = DateTime.UtcNow;
        //                                await assetRepository.UpdateAssetAsync(currentExpenditureAsset);

        //                                //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
        //                                //currentIncomeAsset.Updated = DateTime.UtcNow;
        //                                //await assetRepository.UpdateAssetAsync(currentIncomeAsset);
        //                                #endregion

        //                                #endregion
        //                            }

        //                            return Json(new { result = true, message = localizer["The expenditure has been successfully updated."].Value });
        //                        }
        //                        #endregion
        //                        #region 잘못된 경우
        //                        else
        //                        {
        //                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                        }
        //                        #endregion
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        return Json(new { result = false, error = e.ToString() });
        //                    }
        //                }
        //                else
        //                {
        //                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //                }
        //            }
        //            #endregion

        //            #region 대분류에 없는 값
        //            else
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            #endregion
        //        }
        //        else
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Delete
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> DeleteExpenditure([FromBody] ExpenditureInputViewModel expenditureInputViewModel)
        //{
        //    try
        //    {
        //        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //        var tempExpenditures = await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

        //        if (tempExpenditures == null)
        //        {
        //            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //        }
        //        else
        //        {
        //            var tempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
        //            if (tempExpenditure == null)
        //            {
        //                return Json(new { result = false, error = localizer["Input is invalid"].Value });
        //            }
        //            else
        //            {
        //                #region Validate ProductName's Deletion
        //                var tempExpenditureAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);
        //                if (tempExpenditureAssetToValidate.Deleted)
        //                {
        //                    return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                }
        //                if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
        //                {
        //                    var tempIncomeAssetToValidate = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.MyDepositAsset);
        //                    if (tempIncomeAssetToValidate.Deleted)
        //                    {
        //                        return Json(new { result = false, error = localizer["Actions cannot be executed with assets that have already been deleted."].Value });
        //                    }
        //                }
        //                #endregion

        //                if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
        //                {
        //                    #region 지출 삭제
        //                    await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);
        //                    #endregion

        //                    #region 자산 업데이트
        //                    var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);
        //                    var incomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.MyDepositAsset);

        //                    expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
        //                    expenditureAsset.Updated = DateTime.UtcNow;
        //                    await assetRepository.UpdateAssetAsync(expenditureAsset);

        //                    incomeAsset.Amount -= Math.Abs(tempExpenditure.Amount);
        //                    incomeAsset.Updated = DateTime.UtcNow;
        //                    await assetRepository.UpdateAssetAsync(incomeAsset);
        //                    #endregion

        //                    return Json(new { result = true, message = localizer["The expenditure has been successfully deleted."].Value });
        //                }
        //                else
        //                {
        //                    #region 지출 삭제
        //                    await expenditureRepository.DeleteExpenditureAsync(tempExpenditure);
        //                    #endregion

        //                    #region 자산 업데이트
        //                    var expenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, tempExpenditure.PaymentMethod);

        //                    expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
        //                    expenditureAsset.Updated = DateTime.UtcNow;
        //                    await assetRepository.UpdateAssetAsync(expenditureAsset);
        //                    #endregion

        //                    return Json(new { result = true, message = localizer["The expenditure has been successfully deleted."].Value });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(new { result = false, error = e.ToString() });
        //    }
        //}
        //#endregion

        //#region Excel
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
        //{
        //    // query data from database
        //    await Task.Yield();

        //    List<ExpenditureOutputViewModel> expenditureOutputViewModels = new List<ExpenditureOutputViewModel>();

        //    #region Expenditures

        //    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

        //    foreach (var item in await expenditureRepository.GetExpendituresAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
        //    {
        //        expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
        //        {
        //            Id = item.Id,
        //            MainClass = localizer[item.MainClass.ToString()].Value,
        //            SubClass = localizer[item.SubClass.ToString()].Value,
        //            Contents = item.Contents,
        //            Amount = item.Amount,
        //            PaymentMethod = item.PaymentMethod,
        //            Note = item.Note,
        //            MyDepositAsset = item.MyDepositAsset,
        //            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
        //            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId)
        //        });
        //    }
        //    #endregion

        //    var stream = new MemoryStream();

        //    using (var package = new ExcelPackage(stream))
        //    {
        //        var workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

        //        workSheetMenuOutputViewModels.Row(1).Height = 20;
        //        workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
        //        workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
        //        workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(ExpenditureOutputViewModel.Id).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(ExpenditureOutputViewModel.MainClass).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(ExpenditureOutputViewModel.SubClass).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(ExpenditureOutputViewModel.Contents).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(ExpenditureOutputViewModel.Amount).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(ExpenditureOutputViewModel.PaymentMethod).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(ExpenditureOutputViewModel.Note).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(ExpenditureOutputViewModel.MyDepositAsset).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(ExpenditureOutputViewModel.Created).ToString()].Value;
        //        workSheetMenuOutputViewModels.Cells[1, 10].Value = localizer[nameof(ExpenditureOutputViewModel.Updated).ToString()].Value;

        //        int recordIndex = 2;
        //        foreach (var item in expenditureOutputViewModels)
        //        {
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.Id;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = localizer[item?.MainClass?.ToString() ?? ""].Value;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = localizer[item?.SubClass?.ToString() ?? ""].Value;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.Contents;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Amount;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.PaymentMethod;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Note;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.MyDepositAsset;
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.Created.ToString();
        //            workSheetMenuOutputViewModels.Cells[recordIndex, 10].Value = item.Updated.ToString();

        //            recordIndex++;
        //        }

        //        package.Save();
        //    }
        //    stream.Position = 0;
        //    string excelName = $"{fileName}-{TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, OSTimeZone.DestinationTimeZoneId).ToString("yyyy-MM-dd-HH-mm-ss-fff")}.xlsx";

        //    return File(stream, "application/octet-stream", excelName);
        //}
        //#endregion

        //#endregion
    }
}