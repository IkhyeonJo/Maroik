using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models.ViewModels.Notice;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Options;

namespace Maroik.WebSite.Controllers;

/// <summary>Manages fixed-income and fixed-expenditure notice records (create, read, update, delete, export).</summary>
public class NoticeController : BaseController
{
    private readonly IHtmlLocalizer<NoticeController> _localizer;
    private readonly ILogger<NoticeController> _logger;
    private readonly IAssetService _assetService;
    private readonly IFixedIncomeService _fixedIncomeService;
    private readonly IFixedExpenditureService _fixedExpenditureService;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ISessionService _sessionService;
    private readonly IExcelExportService _excelExportService;

    /// <summary>Initializes a new instance of <see cref="NoticeController"/> with the supplied dependencies.</summary>
    public NoticeController(
        IHtmlLocalizer<NoticeController> localizer,
        ILogger<NoticeController> logger,
        IAssetService assetService,
        IFixedIncomeService fixedIncomeService,
        IFixedExpenditureService fixedExpenditureService,
        IAccountService accountService,
        IDashboardService dashboardService,
        IOptions<ServerSetting> settings,
        ISessionService sessionService,
        IExcelExportService excelExportService)
        : base(accountService, dashboardService, settings, sessionService)
    {
        _localizer = localizer;
        _logger = logger;
        _assetService = assetService;
        _fixedIncomeService = fixedIncomeService;
        _fixedExpenditureService = fixedExpenditureService;
        _settings = settings;
        _sessionService = sessionService;
        _excelExportService = excelExportService;
    }

    private Func<string, string> Localize => key => _localizer[key].Value;
    private int NoticeWindowDays => _settings.Value.NoticeMaturityDateDay;

    #region FixedIncome

    #region Create
    /// <summary>Creates a new fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new FixedIncomeRequest
        {
            Id                        = fixedIncomeInputViewModel.Id,
            MainClass                 = fixedIncomeInputViewModel.MainClass,
            SubClass                  = fixedIncomeInputViewModel.SubClass,
            Content                   = fixedIncomeInputViewModel.Content ?? "",
            Amount                    = fixedIncomeInputViewModel.Amount,
            DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
            DepositMonth              = fixedIncomeInputViewModel.DepositMonth,
            DepositDay                = fixedIncomeInputViewModel.DepositDay,
            MaturityDate              = DateTime.Parse(fixedIncomeInputViewModel.MaturityDate!),
            Note                      = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality             = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await _fixedIncomeService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Read

    /// <summary>Returns the fixed-income grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedIncome(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var account = _sessionService.GetAccount()!;
            var assets  = await _assetService.GetAssetsAsync(account.Email!);
            var incomes = await _fixedIncomeService.GetFixedIncomesAsync(account.Email!);
            var viewModels = incomes
                .ToDisplayViewModels(assets, Localize, account.TimeZoneIanaId!, NoticeWindowDays)
                .ApplySearch(wholeSearch)
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated)
                .AsQueryable();
            return PartialView("_FixedIncomeGrid", viewModels);
        }

        var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
        ViewBag.Assets = (await _assetService.GetAssetsAsync(loginedAccount.Email!))
            .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
        return View();
    }

    /// <summary>Checks whether a fixed-income record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsFixedIncomeExists(int id)
    {
        try
        {
            string email = _sessionService.GetAccount()!.Email!;
            FixedIncomeResponse? item = await _fixedIncomeService.GetByIdAsync(email, id);
            if (item == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            var vm = new FixedIncomeOutputViewModel
            {
                Id                        = item.Id,
                MainClass                 = item.MainClass,
                SubClass                  = item.SubClass,
                Content                   = item.Content,
                Amount                    = item.Amount,
                DepositMonth              = item.DepositMonth,
                DepositDay                = item.DepositDay,
                MaturityDate              = item.MaturityDate.ToString("yyyy-MM-dd"),
                Note                      = item.Note,
                DepositMyAssetProductName = item.DepositMyAssetProductName,
                Unpunctuality             = item.Unpunctuality
            };
            return Json(new { result = true, fixedIncome = vm });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given fixed-income product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetFixedIncomeAmountLabel(string productName)
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
    /// <summary>Updates an existing fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new FixedIncomeRequest
        {
            Id                        = fixedIncomeInputViewModel.Id,
            MainClass                 = fixedIncomeInputViewModel.MainClass,
            SubClass                  = fixedIncomeInputViewModel.SubClass,
            Content                   = fixedIncomeInputViewModel.Content ?? "",
            Amount                    = fixedIncomeInputViewModel.Amount,
            DepositMyAssetProductName = fixedIncomeInputViewModel.DepositMyAssetProductName,
            DepositMonth              = fixedIncomeInputViewModel.DepositMonth,
            DepositDay                = fixedIncomeInputViewModel.DepositDay,
            MaturityDate              = DateTime.Parse(fixedIncomeInputViewModel.MaturityDate!),
            Note                      = fixedIncomeInputViewModel.Note ?? "",
            Unpunctuality             = fixedIncomeInputViewModel.Unpunctuality
        };

        var result = await _fixedIncomeService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Delete
    /// <summary>Deletes an existing fixed-income record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteFixedIncome(
        [FromBody] FixedIncomeInputViewModel fixedIncomeInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email!;
        var result = await _fixedIncomeService.DeleteAsync(email, fixedIncomeInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedIncome has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Excel
    /// <summary>Exports fixed-income records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelFixedIncome(string fileName = "")
    {
        var account = _sessionService.GetAccount()!;
        var assets  = await _assetService.GetAssetsAsync(account.Email!);
        var incomes = await _fixedIncomeService.GetFixedIncomesAsync(account.Email!);
        var stream  = _excelExportService.CreateFixedIncomeExcel(incomes, assets, Localize, account.TimeZoneIanaId!, NoticeWindowDays);
        string name = ExcelFileName(fileName, account.TimeZoneIanaId!);
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }
    #endregion

    #endregion

    #region FixedExpenditure

    #region Create
    /// <summary>Creates a new fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> CreateFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new FixedExpenditureRequest
        {
            Id             = fixedExpenditureInputViewModel.Id,
            MainClass      = fixedExpenditureInputViewModel.MainClass,
            SubClass       = fixedExpenditureInputViewModel.SubClass,
            Content        = fixedExpenditureInputViewModel.Content ?? "",
            Amount         = fixedExpenditureInputViewModel.Amount,
            PaymentMethod  = fixedExpenditureInputViewModel.PaymentMethod,
            MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "",
            DepositMonth   = fixedExpenditureInputViewModel.DepositMonth,
            DepositDay     = fixedExpenditureInputViewModel.DepositDay,
            MaturityDate   = DateTime.Parse(fixedExpenditureInputViewModel.MaturityDate!),
            Note           = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality  = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await _fixedExpenditureService.CreateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully created."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Read

    /// <summary>Returns the fixed-expenditure grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> FixedExpenditure(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var account      = _sessionService.GetAccount()!;
            var assets       = await _assetService.GetAssetsAsync(account.Email!);
            var expenditures = await _fixedExpenditureService.GetFixedExpendituresAsync(account.Email!);
            var viewModels   = expenditures
                .ToDisplayViewModels(assets, Localize, account.TimeZoneIanaId!, NoticeWindowDays)
                .ApplySearch(wholeSearch)
                .OrderByDescending(a => a.Expired).ThenByDescending(m => m.Noticed)
                .ThenByDescending(m => m.Unpunctuality).ThenByDescending(a => a.Created)
                .ThenByDescending(a => a.Updated)
                .AsQueryable();
            return PartialView("_FixedExpenditureGrid", viewModels);
        }

        var loginedAccount = _sessionService.GetAccount() ?? new AccountResponse();
        ViewBag.Assets = (await _assetService.GetAssetsAsync(loginedAccount.Email!))
            .Where(x => !x.Deleted).OrderBy(x => x.ProductName).ToList();
        return View();
    }

    /// <summary>Checks whether a fixed-expenditure record with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsFixedExpenditureExists(int id)
    {
        try
        {
            string email = _sessionService.GetAccount()!.Email!;
            FixedExpenditureResponse? item = await _fixedExpenditureService.GetByIdAsync(email, id);
            if (item == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            var vm = new FixedExpenditureOutputViewModel
            {
                Id             = item.Id,
                MainClass      = item.MainClass,
                SubClass       = item.SubClass,
                Content        = item.Content,
                Amount         = item.Amount,
                DepositMonth   = item.DepositMonth,
                DepositDay     = item.DepositDay,
                MaturityDate   = item.MaturityDate.ToString("yyyy-MM-dd"),
                Note           = item.Note,
                PaymentMethod  = item.PaymentMethod,
                MyDepositAsset = item.MyDepositAsset ?? "",
                Unpunctuality  = item.Unpunctuality
            };
            return Json(new { result = true, fixedExpenditure = vm });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Returns a formatted amount label for the given fixed-expenditure product.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> GetFixedExpenditureAmountLabel(string productName)
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
    /// <summary>Updates an existing fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        string email = _sessionService.GetAccount()!.Email!;

        var request = new FixedExpenditureRequest
        {
            Id             = fixedExpenditureInputViewModel.Id,
            MainClass      = fixedExpenditureInputViewModel.MainClass,
            SubClass       = fixedExpenditureInputViewModel.SubClass,
            Content        = fixedExpenditureInputViewModel.Content ?? "",
            Amount         = fixedExpenditureInputViewModel.Amount,
            PaymentMethod  = fixedExpenditureInputViewModel.PaymentMethod,
            MyDepositAsset = fixedExpenditureInputViewModel.MyDepositAsset ?? "",
            DepositMonth   = fixedExpenditureInputViewModel.DepositMonth,
            DepositDay     = fixedExpenditureInputViewModel.DepositDay,
            MaturityDate   = DateTime.Parse(fixedExpenditureInputViewModel.MaturityDate!),
            Note           = fixedExpenditureInputViewModel.Note ?? "",
            Unpunctuality  = fixedExpenditureInputViewModel.Unpunctuality
        };

        var result = await _fixedExpenditureService.UpdateAsync(email, request);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully updated."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Delete
    /// <summary>Deletes an existing fixed-expenditure record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteFixedExpenditure(
        [FromBody] FixedExpenditureInputViewModel fixedExpenditureInputViewModel)
    {
        string email = _sessionService.GetAccount()!.Email!;
        var result = await _fixedExpenditureService.DeleteAsync(email, fixedExpenditureInputViewModel.Id);
        return result.Success
            ? Json(new { result = true, message = _localizer["The fixedExpenditure has been successfully deleted."].Value })
            : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
    }
    #endregion

    #region Excel
    /// <summary>Exports fixed-expenditure records to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> ExportExcelFixedExpenditure(string fileName = "")
    {
        var account      = _sessionService.GetAccount()!;
        var assets       = await _assetService.GetAssetsAsync(account.Email!);
        var expenditures = await _fixedExpenditureService.GetFixedExpendituresAsync(account.Email!);
        var stream       = _excelExportService.CreateFixedExpenditureExcel(expenditures, assets, Localize, account.TimeZoneIanaId!, NoticeWindowDays);
        string name      = ExcelFileName(fileName, account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }
    #endregion

    #endregion

    private string ExcelFileName(string prefix, string timeZoneIanaId)
        => $"{prefix}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";
}
