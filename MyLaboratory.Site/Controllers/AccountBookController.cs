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

namespace MyLaboratory.Site.Controllers
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
        public AccountBookController(IHtmlLocalizer<AccountBookController> localizer, ILogger<AccountBookController> logger, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IAssetRepository assetRepository, IIncomeRepository incomeRepository)
        {
            this.localizer = localizer;
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
            this.assetRepository = assetRepository;
            this.incomeRepository = incomeRepository;
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
                    HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                    if (await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.ProductName) != null)
                    {
                        return Json(new { result = false, error = localizer["The asset already exists."].Value });
                    }

                    try
                    {
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
                                Note = assetInputViewModel.Note,
                                Deleted = false
                            }
                            );
                    }
                    catch (Exception e)
                    {
                        return Json(new { result = false, error = e.ToString() });
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
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public async Task<IActionResult> Asset(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<AssetOutputViewModel> assetOutputViewModels = new List<AssetOutputViewModel>();

                #region Assets
                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                foreach (var item in await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        assetOutputViewModels.Add(new AssetOutputViewModel()
                        {
                            ProductName = item.ProductName,
                            Item = localizer[item.Item.ToString()].Value,
                            Amount = item.Amount,
                            MonetaryUnit = item.MonetaryUnit,
                            Created = item.Created,
                            Updated = item.Updated,
                            Note = item.Note,
                            Deleted = item.Deleted
                        });
                    }
                    else
                    {
                        if ((item.ProductName?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Item?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Created.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Updated.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Deleted.ToString() ?? "").Contains(wholeSearch))
                        {
                            assetOutputViewModels.Add(new AssetOutputViewModel()
                            {
                                ProductName = item.ProductName,
                                Item = localizer[item.Item.ToString()].Value,
                                Amount = item.Amount,
                                MonetaryUnit = item.MonetaryUnit,
                                Created = item.Created,
                                Updated = item.Updated,
                                Note = item.Note,
                                Deleted = item.Deleted
                            });
                        }
                    }
                }
                #endregion

                var result = assetOutputViewModels.AsQueryable();
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
                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, productName);

                if (tempAsset == null)
                {
                    return Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value });
                }
                else
                {
                    return Json(new { result = true, asset = tempAsset });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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
                ModelState.Remove(nameof(assetInputViewModel.Item));
                ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));

                if (ModelState.IsValid)
                {
                    try
                    {
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                        var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.ProductName);

                        if (tempAsset == null)
                        {
                            return Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value });
                        }
                        else
                        {
                            tempAsset.Amount = assetInputViewModel.Amount;
                            tempAsset.Note = assetInputViewModel.Note;
                            tempAsset.Deleted = assetInputViewModel.Deleted;
                            tempAsset.Updated = DateTime.UtcNow;

                            await assetRepository.UpdateAssetAsync(tempAsset);

                            return Json(new { result = true, message = localizer["The asset has been successfully updated."].Value });
                        }
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

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
        {
            try
            {
                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                var tempAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email, assetInputViewModel.ProductName);

                if (tempAsset == null)
                {
                    return Json(new { result = false, error = localizer["Fail to find the asset by given product name"].Value });
                }
                else
                {
                    tempAsset.Deleted = true;
                    await assetRepository.UpdateAssetAsync(tempAsset);
                    return Json(new { result = true, message = localizer["Successfully deleted the asset"].Value });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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

            List<AssetOutputViewModel> assetOutputViewModels = new List<AssetOutputViewModel>();

            #region Assets

            HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

            foreach (var item in await assetRepository.GetAssetsAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
            {
                assetOutputViewModels.Add(new AssetOutputViewModel()
                {
                    ProductName = item.ProductName,
                    Item = item.Item,
                    Amount = item.Amount,
                    MonetaryUnit = item.MonetaryUnit,
                    Created = item.Created,
                    Updated = item.Updated,
                    Note = item.Note,
                    Deleted = item.Deleted
                });
            }
            #endregion

            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                var workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

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
                foreach (var item in assetOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.ProductName;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = item.Item;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Amount;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.MonetaryUnit;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Created.ToString("d");
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Updated.ToString("d");
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Note;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Deleted;
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion

        #region 수입

        #region Read
        [HttpGet]
        public async Task<IActionResult> Income(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<IncomeOutputViewModel> incomeOutputViewModels= new List<IncomeOutputViewModel>();

                #region Incomes
                HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);

                foreach (var item in await incomeRepository.GetIncomesAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email))
                {
                    if (string.IsNullOrEmpty(wholeSearch))
                    {
                        incomeOutputViewModels.Add(new IncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = item.MainClass,
                            SubClass = item.SubClass,
                            Contents = item.Contents,
                            Amount = item.Amount,
                            DepositAccount = item.DepositAccount,
                            Created = item.Created,
                            Updated = item.Updated,
                            Note = item.Note
                        });
                    }
                    else
                    {
                        if ((item.Id.ToString() ?? "").Contains(wholeSearch) ||
                            (item.MainClass?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.SubClass?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Contents?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Amount.ToString() ?? "").Contains(wholeSearch) ||
                            (item.DepositAccount?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Created.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Updated.ToString() ?? "").Contains(wholeSearch) ||
                            (item.Note?.ToString() ?? "").Contains(wholeSearch))
                        {
                            incomeOutputViewModels.Add(new IncomeOutputViewModel()
                            {
                                Id = item.Id,
                                MainClass = item.MainClass,
                                SubClass = item.SubClass,
                                Contents = item.Contents,
                                Amount = item.Amount,
                                DepositAccount = item.DepositAccount,
                                Created = item.Created,
                                Updated = item.Updated,
                                Note = item.Note
                            });
                        }
                    }
                }
                #endregion

                var result = incomeOutputViewModels.AsQueryable();
                return PartialView("_IncomeGrid", result);
            }
            return View();
        }
        #endregion

        #endregion

        #region 지출

        #region Read
        [HttpGet]
        public IActionResult Expenditure()
        {
            return View();
        }
        #endregion

        #endregion
    }
}