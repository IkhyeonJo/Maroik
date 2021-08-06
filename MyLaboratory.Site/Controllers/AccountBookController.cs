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
        public AccountBookController(IHtmlLocalizer<AccountBookController> localizer, ILogger<AccountBookController> logger, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository, IAssetRepository assetRepository)
        {
            this.localizer = localizer;
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
            this.assetRepository = assetRepository;
        }

        #region 자산

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
                            Item = item.Item,
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
                            (item.AccountEmail?.ToString() ?? "").Contains(wholeSearch) ||
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
                                Item = item.Item,
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
        #endregion

        #endregion

        #region 수입

        #region Read
        [HttpGet]
        public IActionResult Income()
        {
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