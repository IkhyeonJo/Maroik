using Mapster;
﻿using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using Maroik.Core.PostgreSQL.Data;

namespace Maroik.WebSite.Controllers;

public class AccountBookController : Controller
{
    private readonly IHtmlLocalizer<AccountBookController> _localizer;
    private readonly ILogger<AccountBookController> _logger;
    private readonly IAccountRepository _accountRepository;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;
    private readonly ApplicationDbContext _context;

    public AccountBookController(IHtmlLocalizer<AccountBookController> localizer,
        ILogger<AccountBookController> logger, IAccountRepository accountRepository,
        IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository,
        ISubCategoryRepository subCategoryRepository, IAssetRepository assetRepository,
        IIncomeRepository incomeRepository, IExpenditureRepository expenditureRepository,
        IFixedExpenditureRepository fixedExpenditureRepository,
        ApplicationDbContext context)
    {
        _localizer = localizer;
        _logger = logger;
        _accountRepository = accountRepository;
        _hostEnvironment = hostEnvironment;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _assetRepository = assetRepository;
        _incomeRepository = incomeRepository;
        _expenditureRepository = expenditureRepository;
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _context = context;
    }

    #region Asset

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
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);

                if (await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        assetInputViewModel.ProductName) != null)
                {
                    return Json(new { result = false, error = _localizer["The asset already exists."].Value });
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
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    #endregion

                    await _assetRepository.CreateAssetAsync
                    (
                        new AssetRequest()
                        {
                            ProductName = assetInputViewModel.ProductName,
                            AccountEmail = JsonConvert
                                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
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
                catch
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }

                return Json(new
                    { result = true, message = _localizer["The asset has been successfully created."].Value });
            }
            else
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            foreach (AssetResponse item in await _assetRepository.GetAssetsAsync(JsonConvert
                         .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    assetOutputViewModels.Add(new AssetOutputViewModel()
                    {
                        ProductName = item.ProductName,
                        Item = _localizer[item.Item.ToString()].Value,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = item.MonetaryUnit,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note,
                        Deleted = item.Deleted
                    });
                }
                else
                {
                    if ((item.ProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        _localizer[item.Item?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (item.MonetaryUnit?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Deleted.ToString() ?? "").Contains(wholeSearch))
                    {
                        assetOutputViewModels.Add(new AssetOutputViewModel()
                        {
                            ProductName = item.ProductName,
                            Item = _localizer[item.Item.ToString()].Value,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = item.MonetaryUnit,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Note = item.Note,
                            Deleted = item.Deleted
                        });
                    }
                }
            }

            #endregion

            IQueryable<AssetOutputViewModel> result =
                assetOutputViewModels.OrderBy(m => m.ProductName).AsQueryable();
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
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, productName);

            return tempAsset == null
                ? Json(new
                {
                    result = false, error = _localizer["Fail to find the asset by given product name"].Value
                })
                : (IActionResult)Json(new { result = true, asset = tempAsset });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
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
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }

                    #endregion

                    _ = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

                    AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        assetInputViewModel.OriginalProductName);

                    if (tempAsset == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                        {
                            result = false, error = _localizer["Fail to find the asset by given product name"].Value
                        });
                    }
                    else
                    {
                        tempAsset.ProductName = assetInputViewModel.ProductName;
                        tempAsset.Item = assetInputViewModel.Item;
                        tempAsset.Amount = assetInputViewModel.Amount;
                        tempAsset.MonetaryUnit = assetInputViewModel.MonetaryUnit;
                        tempAsset.Note = assetInputViewModel.Note;
                        tempAsset.Deleted = assetInputViewModel.Deleted;
                        tempAsset.Updated = DateTime.UtcNow;

                        if (await _assetRepository.UpdateAssetWithProductNameAsync(tempAsset.Adapt<AssetRequest>(),
                                assetInputViewModel.OriginalProductName) > 0)
                        {
                            _ = await _fixedExpenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                                tempAsset.AccountEmail, assetInputViewModel.OriginalProductName,
                                assetInputViewModel.ProductName);
                            _ = await _expenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                                tempAsset.AccountEmail, assetInputViewModel.OriginalProductName,
                                assetInputViewModel.ProductName);
                            
                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The asset has been successfully updated."].Value
                            });
                        }
                        else
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                assetInputViewModel.ProductName);

            if (tempAsset == null)
            {
                return Json(new
                    { result = false, error = _localizer["Fail to find the asset by given product name"].Value });
            }
            else
            {
                tempAsset.Deleted = true;
                await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());
                return Json(new
                    { result = true, message = _localizer["The asset has been successfully deleted."].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelAsset(string fileName = "")
    {
        await Task.Yield();

        List<AssetOutputViewModel> assetOutputViewModels = new List<AssetOutputViewModel>();

        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

        foreach (AssetResponse item in await _assetRepository.GetAssetsAsync(JsonConvert
                     .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
        {
            assetOutputViewModels.Add(new AssetOutputViewModel()
            {
                ProductName = item.ProductName,
                Item = item.Item,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = item.MonetaryUnit,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note,
                Deleted = item.Deleted
            });
        }

        assetOutputViewModels = assetOutputViewModels.OrderBy(m => m.ProductName).ToList();

        MemoryStream stream = new MemoryStream();
        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Worksheet worksheet = new Worksheet();
            SheetData sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            Sheet sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell(_localizer[nameof(AssetOutputViewModel.ProductName)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Item)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(AssetOutputViewModel.Deleted)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in assetOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.ProductName),
                    CreateCell(_localizer[item?.Item?.ToString() ?? ""].Value),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Note),
                    CreateCell(item.Deleted.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Income

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

                    #region Validate ProductName's Deletion

                    AssetResponse tempAssetToValidate = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        incomeInputViewModel.DepositMyAssetProductName);
                    if (tempAssetToValidate.Deleted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                        {
                            result = false,
                            error = _localizer[
                                "Actions cannot be executed with assets that have already been deleted."].Value
                        });
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
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else if (incomeInputViewModel.MainClass == "IrregularIncome")
                    {
                        if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                            "OtherIncome"))
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }

                    #endregion
                    
                    #region Create income

                    await _incomeRepository.CreateIncomeAsync
                    (
                        new IncomeRequest()
                        {
                            AccountEmail = JsonConvert
                                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                            MainClass = incomeInputViewModel.MainClass,
                            SubClass = incomeInputViewModel.SubClass,
                            Content = incomeInputViewModel.Content ?? "",
                            Amount = Math.Abs(incomeInputViewModel.Amount),
                            DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
                            Created = incomeInputViewModel.Created,
                            Updated = DateTime.UtcNow,
                            Note = incomeInputViewModel.Note ?? ""
                        }
                    );

                    #endregion

                    #region Asset update

                    AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        incomeInputViewModel.DepositMyAssetProductName);
                    tempAsset.Amount += Math.Abs(incomeInputViewModel.Amount);
                    tempAsset.Updated = DateTime.UtcNow;
                    await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());

                    #endregion

                    await transaction.CommitAsync();
                    return Json(new
                        { result = true, message = _localizer["The income has been successfully created."].Value });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            List<AssetResponse> assets = await _assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            foreach (IncomeResponse item in await _incomeRepository.GetIncomesAsync(JsonConvert
                         .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    incomeOutputViewModels.Add(new IncomeOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass.ToString()].Value,
                        SubClass = _localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                            .FirstOrDefault().MonetaryUnit,
                        DepositMyAssetProductName = item.DepositMyAssetProductName,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Note = item.Note
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault()
                            .MonetaryUnit.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DepositMyAssetProductName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch))
                    {
                        incomeOutputViewModels.Add(new IncomeOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass.ToString()].Value,
                            SubClass = _localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName)
                                .FirstOrDefault().MonetaryUnit,
                            DepositMyAssetProductName = item.DepositMyAssetProductName,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Note = item.Note
                        });
                    }
                }
            }

            #endregion

            IQueryable<IncomeOutputViewModel> result = incomeOutputViewModels.OrderByDescending(m => m.Created)
                .ThenByDescending(m => m.Updated).AsQueryable();
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
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;
            List<IncomeResponse> tempIncomes = await _incomeRepository.GetIncomesAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            if (tempIncomes == null)
            {
                return Json(new { result = false, error = _localizer["No income exists"].Value });
            }
            else
            {
                IncomeResponse tempIncome = tempIncomes.Where(a => a.Id == id).FirstOrDefault();

                if (tempIncome == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    tempIncome.Created =
                        tempIncome.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                    return Json(new { result = true, income = tempIncome });
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetIncomeAmountLabel(string productName)
    {
        try
        {
            string label = _localizer["Amount"].Value;

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            AssetResponse asset =
                (await _assetRepository.GetAssetsAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
                .Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
        }
    }

    #endregion

    #region Update

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (ModelState.IsValid)
            {
                try
                {
                    incomeInputViewModel.Amount = Math.Abs(incomeInputViewModel.Amount);
                    _ = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

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
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else if (incomeInputViewModel.MainClass == "IrregularIncome")
                    {
                        if (incomeInputViewModel.SubClass is not ("LaborIncome" or
                            "OtherIncome"))
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }

                    #endregion

                    #region Income & asset update

                    List<IncomeResponse> tempIncomes = await _incomeRepository.GetIncomesAsync(JsonConvert
                        .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

                    if (tempIncomes == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["No income exists"].Value });
                    }
                    else
                    {
                        IncomeResponse tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id)
                            .FirstOrDefault();

                        if (tempIncome == null)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                        else
                        {
                            #region Validate ProductName's Deletion

                            AssetResponse tempAssetToValidate = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, tempIncome.DepositMyAssetProductName);
                            if (tempAssetToValidate.Deleted)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }

                            #endregion

                            string previousTempIncomeDepositMyAssetProductName =
                                tempIncome.DepositMyAssetProductName;
                            decimal previousTempIncomeAmount = tempIncome.Amount;

                            #region Income update

                            tempIncome.MainClass = incomeInputViewModel.MainClass;
                            tempIncome.SubClass = incomeInputViewModel.SubClass;
                            tempIncome.Content = incomeInputViewModel.Content;
                            tempIncome.Amount = incomeInputViewModel.Amount;
                            tempIncome.DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName;
                            tempIncome.Note = incomeInputViewModel.Note;
                            tempIncome.Created = incomeInputViewModel.Created;
                            tempIncome.Updated = DateTime.UtcNow;

                            await _incomeRepository.UpdateIncomeAsync(tempIncome.Adapt<IncomeRequest>());

                            #endregion

                            #region Asset update

                            if (tempIncome.DepositMyAssetProductName ==
                                previousTempIncomeDepositMyAssetProductName) // If same
                            {
                                #region Asset update

                                AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, tempIncome.DepositMyAssetProductName);
                                tempAsset.Amount = tempAsset.Amount - Math.Abs(previousTempIncomeAmount) +
                                                   Math.Abs(tempIncome.Amount);
                                tempAsset.Updated = DateTime.UtcNow;
                                await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());

                                #endregion
                            }
                            else // If different
                            {
                                #region Old asset update

                                AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, previousTempIncomeDepositMyAssetProductName);
                                tempAsset.Amount -= Math.Abs(previousTempIncomeAmount);
                                tempAsset.Updated = DateTime.UtcNow;
                                await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());

                                #endregion

                                #region New asset update

                                tempAsset = await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, tempIncome.DepositMyAssetProductName);
                                tempAsset.Amount += Math.Abs(tempIncome.Amount);
                                tempAsset.Updated = DateTime.UtcNow;
                                await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());

                                #endregion
                            }

                            #endregion

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The income has been successfully updated."].Value
                            });
                        }
                    }

                    #endregion
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
            }
            else
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            List<IncomeResponse> tempIncomes = await _incomeRepository.GetIncomesAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            if (tempIncomes == null)
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                IncomeResponse tempIncome = tempIncomes.Where(a => a.Id == incomeInputViewModel.Id).FirstOrDefault();

                if (tempIncome == null)
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    #region Validate ProductName's Deletion

                    AssetResponse tempAssetToValidate = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        tempIncome.DepositMyAssetProductName);
                    if (tempAssetToValidate.Deleted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                        {
                            result = false,
                            error = _localizer[
                                "Actions cannot be executed with assets that have already been deleted."].Value
                        });
                    }

                    #endregion

                    #region Asset update

                    AssetResponse tempAsset = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        tempIncome.DepositMyAssetProductName);
                    tempAsset.Amount -= Math.Abs(tempIncome.Amount);
                    tempAsset.Updated = DateTime.UtcNow;
                    await _assetRepository.UpdateAssetAsync(tempAsset.Adapt<AssetRequest>());

                    #endregion

                    #region Delete income

                    await _incomeRepository.DeleteIncomeAsync(tempIncome.Adapt<IncomeRequest>());

                    #endregion

                    await transaction.CommitAsync();
                    return Json(new
                        { result = true, message = _localizer["The income has been successfully deleted."].Value });
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelIncome(string fileName = "")
    {
        await Task.Yield();

        List<IncomeOutputViewModel> incomeOutputViewModels = new List<IncomeOutputViewModel>();

        #region Incomes

        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

        List<AssetResponse> assets = await _assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

        foreach (IncomeResponse item in await _incomeRepository.GetIncomesAsync(JsonConvert
                     .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
        {
            incomeOutputViewModels.Add(new IncomeOutputViewModel()
            {
                MainClass = item.MainClass,
                SubClass = item.SubClass,
                Content = item.Content,
                Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                MonetaryUnit = assets.Where(x => x.ProductName == item.DepositMyAssetProductName).FirstOrDefault()
                    .MonetaryUnit,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Note = item.Note
            });
        }

        incomeOutputViewModels = incomeOutputViewModels.OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Worksheet worksheet = new Worksheet();
            SheetData sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            Sheet sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell(_localizer[nameof(IncomeOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.DepositMyAssetProductName)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(IncomeOutputViewModel.Note)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in incomeOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(_localizer[item?.MainClass?.ToString() ?? ""].Value),
                    CreateCell(_localizer[item?.SubClass?.ToString() ?? ""].Value),
                    CreateCell(item.Content),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.DepositMyAssetProductName),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Note)
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion

    #region Expenditure

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (ModelState.IsValid)
            {
                expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);

                #region Validate ProductName's Deletion

                AssetResponse tempExpenditureAssetToValidate = await _assetRepository.GetAssetAsync(
                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                    expenditureInputViewModel.PaymentMethod);
                if (tempExpenditureAssetToValidate.Deleted)
                {
                    await transaction.RollbackAsync();
                    return Json(new
                    {
                        result = false,
                        error = _localizer["Actions cannot be executed with assets that have already been deleted."]
                            .Value
                    });
                }

                if (!string.IsNullOrEmpty(expenditureInputViewModel.MyDepositAsset))
                {
                    AssetResponse tempIncomeAssetToValidate = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        expenditureInputViewModel.MyDepositAsset);
                    if (tempIncomeAssetToValidate.Deleted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                        {
                            result = false,
                            error = _localizer[
                                "Actions cannot be executed with assets that have already been deleted."].Value
                        });
                    }
                }

                #endregion

                #region Main category = 'Regular savings'

                if (expenditureInputViewModel.MainClass == "RegularSavings")
                {
                    if (expenditureInputViewModel.SubClass is "Deposit" or "Investment")
                    {
                        try
                        {
                            if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                        "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                                });
                            }

                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);
                            AssetResponse incomeAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.MyDepositAsset);

                            if (expenditureAsset == null || incomeAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                        .Value
                                });
                            }

                            #region Create expenditure

                            await _expenditureRepository.CreateExpenditureAsync
                            (
                                new ExpenditureRequest()
                                {
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = Math.Abs(expenditureInputViewModel.Amount),
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? ""
                                }
                            );

                            #endregion

                            #region Expenditure asset update

                            expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                            #endregion

                            #region Income asset update

                            incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                            incomeAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(incomeAsset.Adapt<AssetRequest>());

                            #endregion

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The expenditure has been successfully created."].Value
                            });
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Main category = 'Non-consumption expenditure'

                else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
                {
                    if (expenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                    {
                        try
                        {
                            if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                        "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                                });
                            }

                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);
                            AssetResponse incomeAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.MyDepositAsset);

                            if (expenditureAsset == null || incomeAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                        .Value
                                });
                            }

                            #region Create expenditure

                            await _expenditureRepository.CreateExpenditureAsync
                            (
                                new ExpenditureRequest()
                                {
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = Math.Abs(expenditureInputViewModel.Amount),
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = expenditureInputViewModel.MyDepositAsset,
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? ""
                                }
                            );

                            #endregion

                            #region Expenditure asset update

                            expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                            #endregion

                            #region Income asset update

                            incomeAsset.Amount += Math.Abs(expenditureInputViewModel.Amount);
                            incomeAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(incomeAsset.Adapt<AssetRequest>());

                            #endregion

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The expenditure has been successfully created."].Value
                            });
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else if (expenditureInputViewModel.SubClass is "Tax" or
                             "SocialInsurance" or
                             "InterHouseholdTranserExpenses" or
                             "NonProfitOrganizationTransfer")
                    {
                        try
                        {
                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);

                            if (expenditureAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #region Create expenditure

                            await _expenditureRepository.CreateExpenditureAsync
                            (
                                new ExpenditureRequest()
                                {
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = Math.Abs(expenditureInputViewModel.Amount),
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? ""
                                }
                            );

                            #endregion

                            #region Expenditure asset update

                            expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                            #endregion

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The expenditure has been successfully created."].Value
                            });
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Main category = 'Consumption expenditure'

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
                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);

                            if (expenditureAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #region Create expenditure

                            await _expenditureRepository.CreateExpenditureAsync
                            (
                                new ExpenditureRequest()
                                {
                                    AccountEmail = JsonConvert
                                        .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                    MainClass = expenditureInputViewModel.MainClass,
                                    SubClass = expenditureInputViewModel.SubClass,
                                    Content = expenditureInputViewModel.Content ?? "",
                                    Amount = Math.Abs(expenditureInputViewModel.Amount),
                                    PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                    MyDepositAsset = "",
                                    Created = expenditureInputViewModel.Created,
                                    Updated = DateTime.UtcNow,
                                    Note = expenditureInputViewModel.Note ?? ""
                                }
                            );

                            #endregion

                            #region Expenditure asset update

                            expenditureAsset.Amount -= Math.Abs(expenditureInputViewModel.Amount);
                            expenditureAsset.Updated = DateTime.UtcNow;
                            await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                            #endregion

                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The expenditure has been successfully created."].Value
                            });
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Value not in main category

                else
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }

                #endregion
            }
            else
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
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

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            List<AssetResponse> assets = await _assetRepository.GetAssetsAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            foreach (ExpenditureResponse item in await _expenditureRepository.GetExpendituresAsync(JsonConvert
                         .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                    {
                        Id = item.Id,
                        MainClass = _localizer[item.MainClass.ToString()].Value,
                        SubClass = _localizer[item.SubClass.ToString()].Value,
                        Content = item.Content,
                        Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                        MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()
                            .MonetaryUnit,
                        PaymentMethod = item.PaymentMethod,
                        Note = item.Note,
                        MyDepositAsset = item.MyDepositAsset,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                    });
                }
                else
                {
                    if (_localizer[item.MainClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        _localizer[item.SubClass?.ToString() ?? ""].Value.Contains(wholeSearch) ||
                        (item.Content?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Amount.TrimTrailingZeros() ?? "").Contains(wholeSearch) ||
                        (assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault().MonetaryUnit
                            .ToString() ?? "").Contains(wholeSearch) ||
                        (item.PaymentMethod?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Note?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.MyDepositAsset?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch))
                    {
                        expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
                        {
                            Id = item.Id,
                            MainClass = _localizer[item.MainClass.ToString()].Value,
                            SubClass = _localizer[item.SubClass.ToString()].Value,
                            Content = item.Content,
                            Amount = Convert.ToDecimal(item.Amount.TrimTrailingZeros()),
                            MonetaryUnit = assets.Where(x => x.ProductName == item.PaymentMethod).FirstOrDefault()
                                .MonetaryUnit,
                            PaymentMethod = item.PaymentMethod,
                            Note = item.Note,
                            MyDepositAsset = item.MyDepositAsset,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId)
                        });
                    }
                }
            }

            #endregion

            IQueryable<ExpenditureOutputViewModel> result = expenditureOutputViewModels
                .OrderByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
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
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;
            List<ExpenditureResponse> tempExpenditures =
                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            if (tempExpenditures == null)
            {
                return Json(new { result = false, error = _localizer["No expenditure exists"].Value });
            }
            else
            {
                ExpenditureResponse tempExpenditure = tempExpenditures.Where(a => a.Id == id).FirstOrDefault();

                if (tempExpenditure == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    tempExpenditure.Created =
                        tempExpenditure.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId);
                    return Json(new { result = true, expenditure = tempExpenditure });
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetExpenditureAmountLabel(string productName)
    {
        try
        {
            string label = _localizer["Amount"].Value;

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            AssetResponse asset =
                (await _assetRepository.GetAssetsAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
                .Where(x => !x.Deleted).Where(x => x.ProductName == productName).FirstOrDefault() ?? new AssetResponse();

            if (!string.IsNullOrEmpty(asset.MonetaryUnit))
            {
                label = label.GetAmountLabel(asset.MonetaryUnit);
            }

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
        }
    }

    #endregion

    #region Update

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (ModelState.IsValid)
            {
                expenditureInputViewModel.Amount = Math.Abs(expenditureInputViewModel.Amount);
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);

                #region Main category = 'Regular savings'

                if (expenditureInputViewModel.MainClass == "RegularSavings")
                {
                    if (expenditureInputViewModel.SubClass is "Deposit" or "Investment")
                    {
                        try
                        {
                            if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                        "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                                });
                            }

                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);
                            AssetResponse incomeAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.MyDepositAsset);

                            if (expenditureAsset == null || incomeAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                        .Value
                                });
                            }

                            List<ExpenditureResponse> tempExpenditures =
                                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);
                            ExpenditureResponse previousTempExpenditure = tempExpenditures
                                .Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                            ExpenditureResponse currentTempExpenditure = new()
                            {
                                Id = expenditureInputViewModel.Id,
                                AccountEmail = JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel?.Content ?? "",
                                Amount = expenditureInputViewModel.Amount,
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel?.Note ?? "",
                            };

                            #region Validate ProductName's Deletion

                            AssetResponse tempExpenditureAssetToValidate =
                                await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, previousTempExpenditure.PaymentMethod);
                            if (tempExpenditureAssetToValidate.Deleted)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }

                            if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                            {
                                AssetResponse tempIncomeAssetToValidate =
                                    await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);
                                if (tempIncomeAssetToValidate.Deleted)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new
                                    {
                                        result = false,
                                        error = _localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(X))], current expenditure value [(payment method(O)), (my deposit asset(O))]

                            if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod ==
                                    currentTempExpenditure.PaymentMethod) // See else for reference.
                                {
                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);
                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                }
                                else if (previousTempExpenditure.PaymentMethod ==
                                         currentTempExpenditure.MyDepositAsset) // See else for reference.
                                {
                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) +
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());
                                }
                                else
                                {
                                    #region Asset update

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    #region Previous asset

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }

                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(O))], current expenditure value [(payment method(O)), (my deposit asset(O))]

                            else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    if (previousTempExpenditure.MyDepositAsset ==
                                        currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.PaymentMethod ==
                                         currentTempExpenditure.MyDepositAsset)
                                {
                                    if (previousTempExpenditure.MyDepositAsset ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.PaymentMethod)
                                {
                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.MyDepositAsset)
                                {
                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }

                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Invalid case

                            else
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #endregion
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Main category = 'Non-consumption expenditure'

                else if (expenditureInputViewModel.MainClass == "NonConsumerSpending")
                {
                    if (expenditureInputViewModel.SubClass is "PublicPension" or "DebtRepayment")
                    {
                        try
                        {
                            if (expenditureInputViewModel.PaymentMethod == expenditureInputViewModel.MyDepositAsset)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                        "The PaymentMethod and MyDepositAsset value cannot be the same."].Value
                                });
                            }

                            AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.PaymentMethod);
                            AssetResponse incomeAsset = await _assetRepository.GetAssetAsync(
                                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                    .Email, expenditureInputViewModel.MyDepositAsset);

                            if (expenditureAsset == null || incomeAsset == null)
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            if (expenditureAsset.MonetaryUnit != incomeAsset.MonetaryUnit)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit."]
                                        .Value
                                });
                            }

                            List<ExpenditureResponse> tempExpenditures =
                                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);
                            ExpenditureResponse previousTempExpenditure = tempExpenditures
                                .Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                            ExpenditureResponse currentTempExpenditure = new()
                            {
                                Id = expenditureInputViewModel.Id,
                                AccountEmail = JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel?.Content ?? "",
                                Amount = expenditureInputViewModel.Amount,
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = expenditureInputViewModel?.MyDepositAsset ?? "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel?.Note ?? "",
                            };

                            #region Validate ProductName's Deletion

                            AssetResponse tempExpenditureAssetToValidate =
                                await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, previousTempExpenditure.PaymentMethod);
                            if (tempExpenditureAssetToValidate.Deleted)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }

                            if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                            {
                                AssetResponse tempIncomeAssetToValidate =
                                    await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);
                                if (tempIncomeAssetToValidate.Deleted)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new
                                    {
                                        result = false,
                                        error = _localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(X))], current expenditure value [(payment method(O)), (my deposit asset(O))]

                            if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    //#region Asset update

                                    //#region Previous asset
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                    //#endregion

                                    //#region Current asset
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                    //#endregion

                                    //#endregion
                                }
                                else if (previousTempExpenditure.PaymentMethod ==
                                         currentTempExpenditure.MyDepositAsset)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) +
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion

                                    //#region Asset update

                                    //#region Previous asset
                                    //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                    //#endregion

                                    //#region Current asset
                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                    //#endregion

                                    //#endregion
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }

                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(O))], current expenditure value [(payment method(O)), (my deposit asset(O))]

                            else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    if (previousTempExpenditure.MyDepositAsset ==
                                        currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.PaymentMethod ==
                                         currentTempExpenditure.MyDepositAsset)
                                {
                                    if (previousTempExpenditure.MyDepositAsset ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region Asset update

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.PaymentMethod)
                                {
                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.MyDepositAsset)
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) +
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) -
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        currentIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.MyDepositAsset)
                                {
                                    if (previousTempExpenditure.PaymentMethod ==
                                        currentTempExpenditure.PaymentMethod)
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                      Math.Abs(previousTempExpenditure.Amount) -
                                                                      Math.Abs(currentTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                    else
                                    {
                                        #region Asset update

                                        #region Previous asset

                                        AssetResponse pastExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.PaymentMethod);
                                        AssetResponse pastIncomeAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                previousTempExpenditure.MyDepositAsset);

                                        AssetResponse currentExpenditureAsset =
                                            await _assetRepository.GetAssetAsync(
                                                JsonConvert.DeserializeObject<AccountResponse>(
                                                    Encoding.Default.GetString(resultByte)).Email,
                                                currentTempExpenditure.PaymentMethod);

                                        pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                                 Math.Abs(previousTempExpenditure.Amount) +
                                                                 Math.Abs(currentTempExpenditure.Amount);
                                        pastIncomeAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #region Current asset

                                        currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        #endregion

                                        #endregion

                                        //#region Asset update

                                        //#region Previous asset
                                        //var pastExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.PaymentMethod);
                                        //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                        //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                        //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                        //pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                        //pastExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                        //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                        //pastIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#region Current asset
                                        //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                        //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                        //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                        //currentIncomeAsset.Updated = DateTime.UtcNow;
                                        //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());
                                        //#endregion

                                        //#endregion
                                    }
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    AssetResponse currentIncomeAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    currentIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }

                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Invalid case

                            else
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #endregion
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else if (expenditureInputViewModel.SubClass is "Tax" or
                             "SocialInsurance" or
                             "InterHouseholdTranserExpenses" or
                             "NonProfitOrganizationTransfer")
                    {
                        try
                        {
                            List<ExpenditureResponse> tempExpenditures =
                                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);
                            ExpenditureResponse previousTempExpenditure = tempExpenditures
                                .Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                            ExpenditureResponse currentTempExpenditure = new()
                            {
                                Id = expenditureInputViewModel.Id,
                                AccountEmail = JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel?.Content ?? "",
                                Amount = expenditureInputViewModel.Amount,
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel?.Note ?? "",
                            };

                            #region Validate ProductName's Deletion

                            AssetResponse tempExpenditureAssetToValidate =
                                await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, previousTempExpenditure.PaymentMethod);
                            if (tempExpenditureAssetToValidate.Deleted)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }

                            if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                            {
                                AssetResponse tempIncomeAssetToValidate =
                                    await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);
                                if (tempIncomeAssetToValidate.Deleted)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new
                                    {
                                        result = false,
                                        error = _localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(X))], current expenditure value [(payment method(O)), (my deposit asset(X))]

                            if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(O))], current expenditure value [(payment method(O)), (my deposit asset(X))]

                            else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                     string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) -
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Invalid case

                            else
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #endregion
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Main category = 'Consumption expenditure'

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
                            List<ExpenditureResponse> tempExpenditures =
                                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);
                            ExpenditureResponse previousTempExpenditure = tempExpenditures
                                .Where(a => a.Id == expenditureInputViewModel.Id).FirstOrDefault();
                            ExpenditureResponse currentTempExpenditure = new()
                            {
                                Id = expenditureInputViewModel.Id,
                                AccountEmail = JsonConvert
                                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                                MainClass = expenditureInputViewModel.MainClass,
                                SubClass = expenditureInputViewModel.SubClass,
                                Content = expenditureInputViewModel?.Content ?? "",
                                Amount = expenditureInputViewModel.Amount,
                                PaymentMethod = expenditureInputViewModel.PaymentMethod,
                                MyDepositAsset = "",
                                Created = expenditureInputViewModel.Created,
                                Updated = DateTime.UtcNow,
                                Note = expenditureInputViewModel?.Note ?? "",
                            };

                            #region Validate ProductName's Deletion

                            AssetResponse tempExpenditureAssetToValidate =
                                await _assetRepository.GetAssetAsync(
                                    JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte))
                                        .Email, previousTempExpenditure.PaymentMethod);
                            if (tempExpenditureAssetToValidate.Deleted)
                            {
                                await transaction.RollbackAsync();
                                return Json(new
                                {
                                    result = false,
                                    error = _localizer[
                                            "Actions cannot be executed with assets that have already been deleted."]
                                        .Value
                                });
                            }

                            if (!string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset))
                            {
                                AssetResponse tempIncomeAssetToValidate =
                                    await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);
                                if (tempIncomeAssetToValidate.Deleted)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new
                                    {
                                        result = false,
                                        error = _localizer[
                                                "Actions cannot be executed with assets that have already been deleted."]
                                            .Value
                                    });
                                }
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(X))], current expenditure value [(payment method(O)), (my deposit asset(X))]

                            if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    //var pastIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    //pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    //pastIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Previous expenditure value [(payment method(O)), (my deposit asset(O))], current expenditure value [(payment method(O)), (my deposit asset(X))]

                            else if (!string.IsNullOrEmpty(previousTempExpenditure.PaymentMethod) &&
                                     !string.IsNullOrEmpty(previousTempExpenditure.MyDepositAsset) &&
                                     !string.IsNullOrEmpty(currentTempExpenditure.PaymentMethod) &&
                                     string.IsNullOrEmpty(currentTempExpenditure.MyDepositAsset))
                            {
                                #region Expenditure update

                                await _expenditureRepository.UpdateExpenditureAsync(currentTempExpenditure.Adapt<ExpenditureRequest>());

                                #endregion

                                if (previousTempExpenditure.PaymentMethod == currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount = pastExpenditureAsset.Amount +
                                                                  Math.Abs(previousTempExpenditure.Amount) -
                                                                  Math.Abs(currentTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else if (previousTempExpenditure.MyDepositAsset ==
                                         currentTempExpenditure.PaymentMethod)
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    //var currentExpenditureAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount = pastIncomeAsset.Amount -
                                                             Math.Abs(previousTempExpenditure.Amount) -
                                                             Math.Abs(currentTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    //currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    //currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                else
                                {
                                    #region Asset update

                                    #region Previous asset

                                    AssetResponse pastExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            previousTempExpenditure.PaymentMethod);
                                    AssetResponse pastIncomeAsset = await _assetRepository.GetAssetAsync(
                                        JsonConvert.DeserializeObject<AccountResponse>(
                                            Encoding.Default.GetString(resultByte)).Email,
                                        previousTempExpenditure.MyDepositAsset);

                                    AssetResponse currentExpenditureAsset =
                                        await _assetRepository.GetAssetAsync(
                                            JsonConvert.DeserializeObject<AccountResponse>(
                                                Encoding.Default.GetString(resultByte)).Email,
                                            currentTempExpenditure.PaymentMethod);
                                    //var currentIncomeAsset = await assetRepository.GetAssetAsync(JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email, currentTempExpenditure.MyDepositAsset);

                                    pastExpenditureAsset.Amount += Math.Abs(previousTempExpenditure.Amount);
                                    pastExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastExpenditureAsset.Adapt<AssetRequest>());

                                    pastIncomeAsset.Amount -= Math.Abs(previousTempExpenditure.Amount);
                                    pastIncomeAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(pastIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #region Current asset

                                    currentExpenditureAsset.Amount -= Math.Abs(currentTempExpenditure.Amount);
                                    currentExpenditureAsset.Updated = DateTime.UtcNow;
                                    await _assetRepository.UpdateAssetAsync(currentExpenditureAsset.Adapt<AssetRequest>());

                                    //currentIncomeAsset.Amount += Math.Abs(currentTempExpenditure.Amount);
                                    //currentIncomeAsset.Updated = DateTime.UtcNow;
                                    //await assetRepository.UpdateAssetAsync(currentIncomeAsset.Adapt<AssetRequest>());

                                    #endregion

                                    #endregion
                                }
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The expenditure has been successfully updated."].Value
                                });
                            }

                            #endregion

                            #region Invalid case

                            else
                            {
                                await transaction.RollbackAsync();
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            #endregion
                        }
                        catch
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                }

                #endregion

                #region Value not in main category

                else
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }

                #endregion
            }
            else
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            List<ExpenditureResponse> tempExpenditures =
                await _expenditureRepository.GetExpendituresAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            if (tempExpenditures == null)
            {
                await transaction.RollbackAsync();
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                ExpenditureResponse tempExpenditure = tempExpenditures.Where(a => a.Id == expenditureInputViewModel.Id)
                    .FirstOrDefault();
                if (tempExpenditure == null)
                {
                    await transaction.RollbackAsync();
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    #region Validate ProductName's Deletion

                    AssetResponse tempExpenditureAssetToValidate = await _assetRepository.GetAssetAsync(
                        JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                        tempExpenditure.PaymentMethod);
                    if (tempExpenditureAssetToValidate.Deleted)
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                        {
                            result = false,
                            error = _localizer[
                                "Actions cannot be executed with assets that have already been deleted."].Value
                        });
                    }

                    if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
                    {
                        AssetResponse tempIncomeAssetToValidate = await _assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                            tempExpenditure.MyDepositAsset);
                        if (tempIncomeAssetToValidate.Deleted)
                        {
                            await transaction.RollbackAsync();
                            return Json(new
                            {
                                result = false,
                                error = _localizer[
                                    "Actions cannot be executed with assets that have already been deleted."].Value
                            });
                        }
                    }

                    #endregion

                    if (!string.IsNullOrEmpty(tempExpenditure.MyDepositAsset))
                    {
                        #region Delete expenditure

                        await _expenditureRepository.DeleteExpenditureAsync(tempExpenditure.Adapt<ExpenditureRequest>());

                        #endregion

                        #region Asset update

                        AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                            tempExpenditure.PaymentMethod);
                        AssetResponse incomeAsset = await _assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                            tempExpenditure.MyDepositAsset);

                        expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                        incomeAsset.Amount -= Math.Abs(tempExpenditure.Amount);
                        incomeAsset.Updated = DateTime.UtcNow;
                        await _assetRepository.UpdateAssetAsync(incomeAsset.Adapt<AssetRequest>());

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = _localizer["The expenditure has been successfully deleted."].Value
                        });
                    }
                    else
                    {
                        #region Delete expenditure

                        await _expenditureRepository.DeleteExpenditureAsync(tempExpenditure.Adapt<ExpenditureRequest>());

                        #endregion

                        #region Asset update

                        AssetResponse expenditureAsset = await _assetRepository.GetAssetAsync(
                            JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email,
                            tempExpenditure.PaymentMethod);

                        expenditureAsset.Amount += Math.Abs(tempExpenditure.Amount);
                        expenditureAsset.Updated = DateTime.UtcNow;
                        await _assetRepository.UpdateAssetAsync(expenditureAsset.Adapt<AssetRequest>());

                        #endregion

                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true,
                            message = _localizer["The expenditure has been successfully deleted."].Value
                        });
                    }
                }
            }
        }
        catch
        {
            await transaction.RollbackAsync();
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
    {
        await Task.Yield();

        List<ExpenditureOutputViewModel> expenditureOutputViewModels = new List<ExpenditureOutputViewModel>();

        #region Expenditures

        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

        List<AssetResponse> assets = await _assetRepository.GetAssetsAsync(JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

        foreach (ExpenditureResponse item in await _expenditureRepository.GetExpendituresAsync(JsonConvert
                     .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email))
        {
            expenditureOutputViewModels.Add(new ExpenditureOutputViewModel()
            {
                MainClass = _localizer[item.MainClass.ToString()].Value,
                SubClass = _localizer[item.SubClass.ToString()].Value,
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

        expenditureOutputViewModels = expenditureOutputViewModels.OrderByDescending(m => m.Created)
            .ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            Worksheet worksheet = new Worksheet();
            SheetData sheetData = new SheetData();
            worksheet.Append(sheetData);
            worksheetPart.Worksheet = worksheet;

            Sheet sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            Row headerRow = new Row();
            headerRow.Append(
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MainClass)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.SubClass)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Content)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Amount)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MonetaryUnit)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.PaymentMethod)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Note)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.MyDepositAsset)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(ExpenditureOutputViewModel.Updated)].Value)
            );
            sheetData.Append(headerRow);

            foreach (var item in expenditureOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(_localizer[item?.MainClass?.ToString() ?? ""].Value),
                    CreateCell(_localizer[item?.SubClass?.ToString() ?? ""].Value),
                    CreateCell(item.Content),
                    CreateCell(Convert.ToDecimal(item.Amount.TrimTrailingZeros()).ToString()),
                    CreateCell(item.MonetaryUnit),
                    CreateCell(item.PaymentMethod),
                    CreateCell(item.Note),
                    CreateCell(item.MyDepositAsset),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString())
                );
                sheetData.Append(dataRow);
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        string excelName =
            $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

        return File(stream, "application/octet-stream", excelName);
    }

    #endregion

    #endregion
}