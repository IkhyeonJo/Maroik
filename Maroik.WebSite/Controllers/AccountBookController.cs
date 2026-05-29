using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Services;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.AccountBook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>
/// Manages account-book records: assets, income transactions, and expenditure transactions
/// (create, read, update, delete, export).
/// </summary>
public class AccountBookController : BaseController
{
    private readonly IHtmlLocalizer<AccountBookController> _localizer;
    private readonly ILogger<AccountBookController> _logger;
    private readonly IAssetService _assetService;
    private readonly IIncomeService _incomeService;
    private readonly IExpenditureService _expenditureService;
    private readonly ISessionService _sessionService;
    private readonly IExcelExportService _excelExportService;

    /// <summary>Initializes a new instance of <see cref="AccountBookController"/> with the supplied dependencies.</summary>
    public AccountBookController(
        IHtmlLocalizer<AccountBookController> localizer,
        ILogger<AccountBookController> logger,
        IAssetService assetService,
        IIncomeService incomeService,
        IExpenditureService expenditureService,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService,
        IExcelExportService excelExportService)
        : base(accountService, dashboardService, serverSettings, sessionService)
    {
        _localizer = localizer;
        _logger = logger;
        _assetService = assetService;
        _incomeService = incomeService;
        _expenditureService = expenditureService;
        _sessionService = sessionService;
        _excelExportService = excelExportService;
    }

    private Func<string, string> Localize => key => _localizer[key].Value;

    #region Asset

    #region Create

    /// <summary>Creates a new asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new AssetRequest
        {
            ProductName  = assetInputViewModel.ProductName,
            Item         = assetInputViewModel.Item,
            Amount       = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note         = assetInputViewModel.Note ?? "",
            Deleted      = false
        };

        var result = await _assetService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the asset grid (AJAX) or the full asset page.</summary>
    [HttpGet]
    public async Task<IActionResult> Asset(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var account = _sessionService.GetAccount()!;
            var assets  = await _assetService.GetAssetsAsync(account.Email!);
            var viewModels = assets
                .ToDisplayViewModels(Localize, account.TimeZoneIanaId!)
                .ApplySearch(wholeSearch)
                .OrderBy(m => m.ProductName)
                .AsQueryable();
            return PartialView("_AssetGrid", viewModels);
        }

        return await AssetPageAsync();
    }

    /// <summary>Checks whether an asset with the given product name exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsAssetExists(string productName)
    {
        try
        {
            AssetResponse? asset = await _assetService.GetAssetAsync(
                _sessionService.GetAccount()!.Email!, productName);

            return asset == null
                ? Json(new { result = false, error = _localizer["Fail to find the asset by given product name"].Value })
                : Json(new { result = true, asset });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        _ = ModelState.Remove(nameof(assetInputViewModel.MonetaryUnit));
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new AssetRequest
        {
            ProductName  = assetInputViewModel.ProductName,
            Item         = assetInputViewModel.Item,
            Amount       = assetInputViewModel.Amount,
            MonetaryUnit = assetInputViewModel.MonetaryUnit,
            Note         = assetInputViewModel.Note,
            Deleted      = assetInputViewModel.Deleted
        };

        var result = await _assetService.UpdateAsync(email, request, assetInputViewModel.OriginalProductName!);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Delete

    /// <summary>Soft-deletes an existing asset record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteAsset([FromBody] AssetInputViewModel assetInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email!;
        var result = await _assetService.DeleteAsync(email, assetInputViewModel.ProductName!);
        return result.Success
            ? Json(new { result = true, message = _localizer["The asset has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Excel

    /// <summary>Exports asset records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelAsset(string fileName = "")
    {
        var account = _sessionService.GetAccount()!;
        var assets  = await _assetService.GetAssetsAsync(account.Email!);
        var stream  = _excelExportService.CreateAssetExcel(assets, Localize, account.TimeZoneIanaId!);
        string name = ExcelFileName(fileName, account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }

    #endregion

    #endregion

    #region Income

    #region Create

    /// <summary>Creates a new income transaction.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new IncomeRequest
        {
            MainClass                 = incomeInputViewModel.MainClass,
            SubClass                  = incomeInputViewModel.SubClass,
            Content                   = incomeInputViewModel.Content ?? "",
            Amount                    = incomeInputViewModel.Amount,
            DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
            Created                   = incomeInputViewModel.Created,
            Note                      = incomeInputViewModel.Note ?? ""
        };

        var result = await _incomeService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the income grid (AJAX) or the full income page.</summary>
    [HttpGet]
    public async Task<IActionResult> Income(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var account = _sessionService.GetAccount()!;
            var assets  = await _assetService.GetAssetsAsync(account.Email!);
            var incomes = await _incomeService.GetIncomesAsync(account.Email!);
            var viewModels = incomes
                .ToDisplayViewModels(assets, Localize, account.TimeZoneIanaId!)
                .ApplySearch(wholeSearch)
                .OrderByDescending(m => m.Created)
                .ThenByDescending(m => m.Updated)
                .AsQueryable();
            return PartialView("_IncomeGrid", viewModels);
        }

        return await IncomePageAsync();
    }

    /// <summary>Checks whether an income record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsIncomeExists(int id)
    {
        try
        {
            string email = _sessionService.GetAccount()!.Email!;
            string tz    = _sessionService.GetAccount()!.TimeZoneIanaId!;

            IncomeResponse? income = await _incomeService.GetByIdAsync(email, id);
            if (income == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            income.Created = income.Created.ConvertTimeByTimeZoneIanaId(tz);
            return Json(new { result = true, income });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given income product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetIncomeAmountLabel(string productName)
    {
        try
        {
            string baseLabel = _localizer["Amount"].Value;
            AssetResponse? asset = await _assetService.GetAssetAsync(
                _sessionService.GetAccount()!.Email!, productName);

            string label = !string.IsNullOrEmpty(asset?.MonetaryUnit)
                ? baseLabel.GetAmountLabel(asset.MonetaryUnit)
                : baseLabel;

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new IncomeRequest
        {
            Id                        = incomeInputViewModel.Id,
            MainClass                 = incomeInputViewModel.MainClass,
            SubClass                  = incomeInputViewModel.SubClass,
            Content                   = incomeInputViewModel.Content,
            Amount                    = incomeInputViewModel.Amount,
            DepositMyAssetProductName = incomeInputViewModel.DepositMyAssetProductName,
            Created                   = incomeInputViewModel.Created,
            Note                      = incomeInputViewModel.Note
        };

        var result = await _incomeService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Delete

    /// <summary>Deletes an existing income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteIncome([FromBody] IncomeInputViewModel incomeInputViewModel)
    {
        string email  = _sessionService.GetAccount()!.Email!;
        var result    = await _incomeService.DeleteAsync(email, incomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The income has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Excel

    /// <summary>Exports income records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelIncome(string fileName = "")
    {
        var account = _sessionService.GetAccount()!;
        var assets  = await _assetService.GetAssetsAsync(account.Email!);
        var incomes = await _incomeService.GetIncomesAsync(account.Email!);
        var stream  = _excelExportService.CreateIncomeExcel(incomes, assets, Localize, account.TimeZoneIanaId!);
        string name = ExcelFileName(fileName, account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }

    #endregion

    #endregion

    #region Expenditure

    #region Create

    /// <summary>Creates a new expenditure transaction.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new ExpenditureRequest
        {
            MainClass      = expenditureInputViewModel.MainClass,
            SubClass       = expenditureInputViewModel.SubClass,
            Content        = expenditureInputViewModel.Content ?? "",
            Amount         = expenditureInputViewModel.Amount,
            PaymentMethod  = expenditureInputViewModel.PaymentMethod,
            MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
            Created        = expenditureInputViewModel.Created,
            Note           = expenditureInputViewModel.Note ?? ""
        };

        var result = await _expenditureService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Read

    /// <summary>Returns the expenditure grid (AJAX) or the full expenditure page.</summary>
    [HttpGet]
    public async Task<IActionResult> Expenditure(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var account      = _sessionService.GetAccount()!;
            var assets       = await _assetService.GetAssetsAsync(account.Email!);
            var expenditures = await _expenditureService.GetExpendituresAsync(account.Email!);
            var viewModels   = expenditures
                .ToDisplayViewModels(assets, Localize, account.TimeZoneIanaId!)
                .ApplySearch(wholeSearch)
                .OrderByDescending(m => m.Created)
                .ThenByDescending(m => m.Updated)
                .AsQueryable();
            return PartialView("_ExpenditureGrid", viewModels);
        }

        return await ExpenditurePageAsync();
    }

    /// <summary>Checks whether an expenditure record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsExpenditureExists(int id)
    {
        try
        {
            string email = _sessionService.GetAccount()!.Email!;
            string tz    = _sessionService.GetAccount()!.TimeZoneIanaId!;

            ExpenditureResponse? expenditure = await _expenditureService.GetByIdAsync(email, id);
            if (expenditure == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            expenditure.Created = expenditure.Created.ConvertTimeByTimeZoneIanaId(tz);
            return Json(new { result = true, expenditure });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given expenditure product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetExpenditureAmountLabel(string productName)
    {
        try
        {
            string baseLabel = _localizer["Amount"].Value;
            AssetResponse? asset = await _assetService.GetAssetAsync(
                _sessionService.GetAccount()!.Email!, productName);

            string label = !string.IsNullOrEmpty(asset?.MonetaryUnit)
                ? baseLabel.GetAmountLabel(asset.MonetaryUnit)
                : baseLabel;

            return Json(new { result = true, label });
        }
        catch
        {
            return Json(new { result = false, label = _localizer["Amount"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new ExpenditureRequest
        {
            Id             = expenditureInputViewModel.Id,
            MainClass      = expenditureInputViewModel.MainClass,
            SubClass       = expenditureInputViewModel.SubClass,
            Content        = expenditureInputViewModel.Content ?? "",
            Amount         = expenditureInputViewModel.Amount,
            PaymentMethod  = expenditureInputViewModel.PaymentMethod,
            MyDepositAsset = expenditureInputViewModel.MyDepositAsset ?? "",
            Created        = expenditureInputViewModel.Created,
            Note           = expenditureInputViewModel.Note ?? ""
        };

        var result = await _expenditureService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Delete

    /// <summary>Deletes an existing expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteExpenditure(
        [FromBody] ExpenditureInputViewModel expenditureInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email!;
        var result   = await _expenditureService.DeleteAsync(email, expenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The expenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey!].Value });
    }

    #endregion

    #region Excel

    /// <summary>Exports expenditure records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelExpenditure(string fileName = "")
    {
        var account      = _sessionService.GetAccount()!;
        var assets       = await _assetService.GetAssetsAsync(account.Email!);
        var expenditures = await _expenditureService.GetExpendituresAsync(account.Email!);
        var stream       = _excelExportService.CreateExpenditureExcel(expenditures, assets, Localize, account.TimeZoneIanaId!);
        string name      = ExcelFileName(fileName, account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }

    #endregion

    #endregion

    #region Helpers

    private async Task<IActionResult> AssetPageAsync()
    {
        var account = _sessionService.GetAccount() ?? new AccountResponse();
        string tz   = account.TimeZoneIanaId ?? "UTC";
        var assets  = (await _assetService.GetAssetsAsync(account.Email!))
            .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
        SetCurrentTimeViewBag(tz, assets);
        return View();
    }

    private async Task<IActionResult> IncomePageAsync()
    {
        var account = _sessionService.GetAccount() ?? new AccountResponse();
        string tz   = account.TimeZoneIanaId ?? "UTC";
        var assets  = (await _assetService.GetAssetsAsync(account.Email!))
            .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
        SetCurrentTimeViewBag(tz, assets);
        return View();
    }

    private async Task<IActionResult> ExpenditurePageAsync()
    {
        var account = _sessionService.GetAccount() ?? new AccountResponse();
        string tz   = account.TimeZoneIanaId ?? "UTC";
        var assets  = (await _assetService.GetAssetsAsync(account.Email!))
            .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
        SetCurrentTimeViewBag(tz, assets);
        return View();
    }

    private void SetCurrentTimeViewBag(string tz, List<AssetResponse> assets)
    {
        DateTime ct = DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(tz);
        ViewBag.Assets        = assets;
        ViewBag.CurrentDate   = ct.ToString("yyyy-MM-dd");
        ViewBag.CurrentHour   = ct.Hour;
        ViewBag.CurrentMinute = ct.Minute;
        ViewBag.CurrentSecond = ct.Second;
    }

    private string ExcelFileName(string fileNamePrefix, string timeZoneIanaId)
        => $"{fileNamePrefix}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

    #endregion
}
