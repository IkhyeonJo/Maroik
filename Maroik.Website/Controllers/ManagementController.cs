using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Misc.Extensions;
using Maroik.Core.Contract.Misc.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Website.Attributes;
using Maroik.Website.Contracts;
using Maroik.Website.Extensions;
using Maroik.Website.Mappings;
// ReSharper disable ConvertToConstant.Local
using Maroik.Website.Models;
using Maroik.Website.Models.ViewModels.Management;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Controllers;

/// <summary>
/// Admin-only controller for managing user profiles, accounts, and the navigation menu
/// (categories and sub-categories).
/// </summary>
public class ManagementController : Controller
{
    private readonly IRsaService _rsa;
    private readonly IHtmlLocalizer<ManagementController> _localizer;
    private readonly ILogger<ManagementController> _logger;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IProfileService _profileService;
    private readonly IAccountService _accountService;
    private readonly IManagementAccountService _managementAccountService;
    private readonly IMenuService _menuService;
    private readonly IBoardService _boardService;
    private readonly ISessionService _sessionService;
    private readonly IExcelExportService _excelExportService;
    private readonly IDistributedCache _cache;
    private readonly IOptions<ServerSetting> _serverSettings;
    private readonly Func<string, string> _localize;

    /// <summary>Initializes a new instance of <see cref="ManagementController"/> with the supplied dependencies.</summary>
    public ManagementController(
        IHtmlLocalizer<ManagementController> localizer,
        ILogger<ManagementController> logger,
        IHostEnvironment hostEnvironment,
        IProfileService profileService,
        IAccountService accountService,
        IManagementAccountService managementAccountService,
        IMenuService menuService,
        IBoardService boardService,
        IRsaService rsa,
        IOptions<ServerSetting> serverSettings,
        ISessionService sessionService,
        IExcelExportService excelExportService,
        IDistributedCache cache)
    {
        _rsa = rsa;
        _sessionService = sessionService;
        _localizer = localizer;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
        _profileService = profileService;
        _accountService = accountService;
        _managementAccountService = managementAccountService;
        _menuService = menuService;
        _boardService = boardService;
        _excelExportService = excelExportService;
        _cache = cache;
        _serverSettings = serverSettings;
        _localize = key => _localizer[key].Value;
    }

    #region Profile

    #region Read

    /// <summary>Displays the current user's profile page.</summary>
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        string? loggedInAccountTimeZoneIanaId = _sessionService.GetAccount()!.TimeZoneIanaId;
        AccountResponse? tempAccount = await _profileService.GetProfileAsync(_sessionService.GetAccount()!.Email!, HttpContext.RequestAborted);

        return View(new ProfileOutputViewModel()
        {
            Email = tempAccount!.Email,
            AvatarImagePath = tempAccount.AvatarImagePath,
            Nickname = tempAccount.Nickname,
            Created = tempAccount.Created.ConvertTimeByTimeZoneIanaId(loggedInAccountTimeZoneIanaId!),
            TimeZoneIanaId = tempAccount.TimeZoneIanaId
        });
    }

    #endregion

    #region Update

    #region ProfileAvatar

    /// <summary>Updates the current user's avatar image.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateProfileAvatar(ProfileInputViewModel profileInputViewModel)
    {
        if (profileInputViewModel.ProfileAvatarFiles == null || profileInputViewModel.ProfileAvatarFiles.Count == 0)
        {
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file"].Value });
        }

        IFormFile file = profileInputViewModel.ProfileAvatarFiles[0];

        if (file.Length <= 0 || file.Length > _serverSettings.Value.MaxAttachedFileSizeBytes)
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than {0}MB.", _serverSettings.Value.MaxAttachedFileSizeBytes / (1024 * 1024)].Value });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed"].Value });

        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        string avatarDirectoryPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", "Avatar");
        string? email = _sessionService.GetAccount()!.Email;

        ServiceResult avatarResult = await _profileService.UploadAndUpdateAvatarAsync(email!, imageBytes, extension, avatarDirectoryPath, HttpContext.RequestAborted);

        if (!avatarResult.Success)
        {
            return avatarResult.ErrorKey switch
            {
                "virus-detected" => BadRequest("File may be infected with a virus."),
                "svg-not-allowed" => Ok(new { result = false, errorMessage = _localizer["SVG format is not allowed"].Value }),
                "invalid-image" => Ok(new { result = false, errorMessage = _localizer["Invalid image file"].Value }),
                _ => Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value })
            };
        }

        return Ok(new { result = true });
    }

    #endregion

    #region ProfileTimeZone

    /// <summary>Updates the current user's time-zone setting.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateProfileTimeZone(ProfileInputViewModel profileInputViewModel)
    {
        _ = ModelState.Remove(nameof(profileInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(profileInputViewModel.Password));
        _ = ModelState.Remove(nameof(profileInputViewModel.NewPassword));

        if (!ModelState.IsValid)
        {
            return RedirectToAction("Profile", "Management");
        }

        try
        {
            string? email = _sessionService.GetAccount()!.Email;
            await _profileService.UpdateTimeZoneAsync(email!, profileInputViewModel.TimeZoneIanaId!, HttpContext.RequestAborted);

            // Refresh session with updated timezone
            AccountResponse? updatedAccount = await _profileService.GetProfileAsync(email!, HttpContext.RequestAborted);
            _sessionService.RemoveAccount();
            updatedAccount!.HashedPassword = null;
            _sessionService.SetAccount(updatedAccount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update time zone for account {AccountEmail}", _sessionService.GetAccount()?.Email);
        }

        return RedirectToAction("Profile", "Management");
    }

    #endregion

    #region ProfilePassword

    /// <summary>Changes the current user's password.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateProfilePassword([FromBody] ProfileInputViewModel profileInputViewModel)
    {
        _ = ModelState.Remove(nameof(profileInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(profileInputViewModel.TimeZoneIanaId));

        if (!ModelState.IsValid)
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });

        try
        {
            string? email = _sessionService.GetAccount()!.Email;

            ServiceResult result = await _profileService.UpdatePasswordAsync(
                email!,
                profileInputViewModel.Password ?? "",
                profileInputViewModel.NewPassword ?? "",
                HttpContext.RequestAborted);

            return result.Success
                ? Json(new { result = true, message = _localizer["Password change successfully done."].Value })
                : Json(new { result = false, error = _localizer[result.ErrorKey].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update password for account {AccountEmail}", _sessionService.GetAccount()?.Email);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion

    #region Account

    #region Create

    /// <summary>Creates a new user account (admin action).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> CreateAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(accountInputViewModel.AvatarImagePath));
            _ = ModelState.Remove(nameof(accountInputViewModel.RegistrationToken));
            _ = ModelState.Remove(nameof(accountInputViewModel.ResetPasswordToken));
            _ = ModelState.Remove(nameof(accountInputViewModel.Message));
            // TimeZoneIanaId's [Required] would otherwise reject an empty value before the
            // friendlier "Please select time zone" check further down ever runs.
            _ = ModelState.Remove(nameof(accountInputViewModel.TimeZoneIanaId));

            if (!ModelState.IsValid)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }

            #region Email format check

            if (Email.Create(accountInputViewModel.Email).IsError) // Invalid email format
            {
                return Json(new { result = false, error = _localizer["Invalid email form"].Value });
            }

            #endregion

            #region Password format check

            if (!PasswordPolicy.IsValid(accountInputViewModel.Password))
            {
                return Json(new
                {
                    result = false,
                    error = _localizer[
                            "Password must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)"]
                        .Value
                });
            }

            #endregion

            #region Nickname format check

            if (string.IsNullOrEmpty(accountInputViewModel.Nickname))
            {
                return Json(new { result = false, error = _localizer["Please enter Nickname"].Value });
            }

            if (accountInputViewModel.Nickname.Length is not (>= 1 and <= 255))
            {
                return Json(new
                {
                    result = false,
                    error = _localizer["Nickname is must be between 1 and 255 characters"].Value
                });
            }

            #endregion

            #region Role check

            if (accountInputViewModel.Role is not (Role.Admin or Role.User))
            {
                accountInputViewModel.Role = Role.User;
            }

            #endregion

            #region TimeZone format check

            if (string.IsNullOrEmpty(accountInputViewModel.TimeZoneIanaId))
            {
                return Json(new { result = false, error = _localizer["Please select time zone"].Value });
            }

            #endregion

            #region Create account

            ServiceResult createResult = await _managementAccountService.CreateAccountAsync(new AccountRequest
            {
                Email = accountInputViewModel.Email,
                HashedPassword = accountInputViewModel.Password, // service will hash
                Nickname = accountInputViewModel.Nickname,
                AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                Role = accountInputViewModel.Role,
                TimeZoneIanaId = accountInputViewModel.TimeZoneIanaId,
                Locked = false,
                LoginAttempt = 0,
                EmailConfirmed = true,
                AgreedServiceTerms = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
                Message = EnumHelper.GetDescription(AccountMessage.Success),
                Deleted = false
            }, HttpContext.RequestAborted);

            return createResult.Success
                ? Json(new { result = true, message = _localizer["Account create successfully done."].Value })
                : Json(new { result = false, error = _localizer[createResult.ErrorKey].Value });

            #endregion

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create account {AccountEmail}", accountInputViewModel.Email);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    /// <summary>Returns the account management grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> Account(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] != "XMLHttpRequest")
#pragma warning restore ASP0015
        {
            return View();
        }

        string tz = _sessionService.GetAccount()!.TimeZoneIanaId!;
        var accounts = await _managementAccountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var viewModels = accounts
            .ToDisplayViewModels(tz)
            .ApplySearch(wholeSearch)
            .OrderBy(m => m.Email).ThenBy(m => m.Nickname)
            .ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated)
            .AsQueryable();
        return PartialView("_AccountGrid", viewModels);

    }

    /// <summary>Checks whether an account with the given email exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsAccountExists(string email)
    {
        try
        {
            AccountResponse? tempAccount = await _managementAccountService.GetAccountByEmailAsync(email, HttpContext.RequestAborted);

            return tempAccount == null
                ? Json(new
                {
                    result = false,
                    error = _localizer["Fail to find the account by given email address"].Value
                })
                : (IActionResult)Json(new { result = true, account = tempAccount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check account existence for email {AccountEmail}", email);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing account's fields.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(accountInputViewModel.Nickname));
            _ = ModelState.Remove(nameof(accountInputViewModel.AvatarImagePath));
            _ = ModelState.Remove(nameof(accountInputViewModel.RegistrationToken));
            _ = ModelState.Remove(nameof(accountInputViewModel.ResetPasswordToken));
            // Password/Message are optional on update — an empty Password means "leave the
            // current password unchanged" (the service only calls ChangePassword when non-empty),
            // and Message has no [Required]-worthy business meaning here.
            _ = ModelState.Remove(nameof(accountInputViewModel.Password));
            _ = ModelState.Remove(nameof(accountInputViewModel.Message));

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            if (accountInputViewModel.Role is not (Role.Admin or Role.User))
            {
                accountInputViewModel.Role = Role.User;
            }

            ServiceResult updateResult = await _managementAccountService.UpdateAccountAsync(
                new AccountRequest
                {
                    Email = accountInputViewModel.Email,
                    Role = accountInputViewModel.Role,
                    TimeZoneIanaId = accountInputViewModel.TimeZoneIanaId,
                    Locked = accountInputViewModel.Locked,
                    EmailConfirmed = accountInputViewModel.EmailConfirmed,
                    AgreedServiceTerms = accountInputViewModel.AgreedServiceTerms,
                    Message = accountInputViewModel.Message,
                    Deleted = accountInputViewModel.Deleted
                },
                accountInputViewModel.Password, // new password (plain); service hashes it
                HttpContext.RequestAborted);

            return updateResult.Success
                ? Json(new { result = true, message = _localizer["Successfully updated the account"].Value })
                : Json(new { result = false, error = _localizer[updateResult.ErrorKey].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update account {AccountEmail}", accountInputViewModel.Email);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    /// <summary>Soft-deletes an existing account.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            ServiceResult deleteResult = await _managementAccountService.DeleteAccountAsync(accountInputViewModel.Email!, HttpContext.RequestAborted);
            return deleteResult.Success
                ? Json(new { result = true, message = _localizer["Successfully deleted the account"].Value })
                : Json(new { result = false, error = _localizer[deleteResult.ErrorKey].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete account {AccountEmail}", accountInputViewModel.Email);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    /// <summary>Exports the account list to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelAccount(string fileName = "")
    {
        var account = _sessionService.GetAccount()!;
        var accounts = await _managementAccountService.GetAllAccountsAsync(HttpContext.RequestAborted);
        var stream = _excelExportService.CreateAccountExcel(accounts, _localize, account.TimeZoneIanaId!);
        string name = fileName.ToExcelFileName(account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }

    #endregion

    #endregion

    #region Menu

    #region Create

    /// <summary>Creates a new navigation category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> CreateCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult createCatResult = await _menuService.CreateCategoryAsync(new CategoryRequest
            {
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Controller = menuInputViewModel.Controller,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);
            if (!createCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully created the category"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create menu category {CategoryName}", menuInputViewModel.Name);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Creates a new navigation sub-category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> CreateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult createSubCatResult = await _menuService.CreateSubCategoryAsync(new SubCategoryRequest
            {
                CategoryId = menuInputViewModel.CategoryId,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);
            if (!createSubCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully created the subCategory"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create menu sub-category {SubCategoryName}", menuInputViewModel.Name);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    /// <summary>Returns the menu management grid (AJAX) or the full page.</summary>
    [HttpGet]
    public async Task<IActionResult> Menu(string wholeSearch)
    {
#pragma warning disable ASP0015
        if (HttpContext.Request.Headers["X-Requested-With"] != "XMLHttpRequest") // ajax
#pragma warning restore ASP0015
        {
            return View();
        }

        List<MenuOutputViewModel> menuOutputViewModels =
        [
            .. from item in await _menuService.GetAllCategoriesAsync(HttpContext.RequestAborted)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) ||
                  (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) ||
                  (item.IconPath ?? "").Contains(wholeSearch) || (item.Controller ?? "").Contains(wholeSearch) ||
                  (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) ||
                  item.Order.ToString().Contains(wholeSearch)
            select new MenuOutputViewModel
            {
                Id = item.Id,
                CategoryId = null,
                Name = item.Name,
                DisplayName = item.DisplayName,
                IconPath = item.IconPath,
                Controller = item.Controller,
                Action = item.Action,
                Role = item.Role,
                Order = item.Order
            },

            .. from item in await _menuService.GetAllSubCategoriesAsync(HttpContext.RequestAborted)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) ||
                  item.CategoryId.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) ||
                  (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) ||
                  (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) ||
                  item.Order.ToString().Contains(wholeSearch)
            select new MenuOutputViewModel
            {
                Id = item.Id,
                CategoryId = item.CategoryId,
                Name = item.Name,
                DisplayName = item.DisplayName,
                IconPath = item.IconPath,
                Controller = null,
                Action = item.Action,
                Role = item.Role,
                Order = item.Order
            }
        ];

        IQueryable<MenuOutputViewModel> result = menuOutputViewModels.OrderByDescending(m => m.Id)
            .ThenByDescending(m => m.CategoryId).AsQueryable();
        return PartialView("_MenuGrid", result);

    }

    /// <summary>Checks whether a category with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsCategoryExists(int id)
    {
        try
        {
            CategoryResponse? category = await _menuService.GetCategoryByIdAsync(id, HttpContext.RequestAborted);
            if (category == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            MenuOutputViewModel vm = new()
            {
                Id = category.Id,
                CategoryId = null,
                Name = category.Name,
                DisplayName = category.DisplayName,
                IconPath = category.IconPath,
                Controller = category.Controller,
                Action = category.Action,
                Role = category.Role,
                Order = category.Order
            };
            return Json(new { result = true, category = vm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check category existence for id {CategoryId}", id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Checks whether a sub-category with the given ID exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsSubCategoryExists(int id)
    {
        try
        {
            SubCategoryResponse? subCategory = await _menuService.GetSubCategoryByIdAsync(id, HttpContext.RequestAborted);
            if (subCategory == null)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            MenuOutputViewModel vm = new()
            {
                Id = subCategory.Id,
                CategoryId = subCategory.CategoryId,
                Name = subCategory.Name,
                DisplayName = subCategory.DisplayName,
                IconPath = subCategory.IconPath,
                Controller = null,
                Action = subCategory.Action,
                Role = subCategory.Role,
                Order = subCategory.Order
            };
            return Json(new { result = true, subCategory = vm });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check sub-category existence for id {SubCategoryId}", id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Update

    /// <summary>Updates an existing navigation category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult updateCatResult = await _menuService.UpdateCategoryAsync(new CategoryRequest
            {
                Id = menuInputViewModel.Id,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Controller = menuInputViewModel.Controller,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);
            if (!updateCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully updated the category"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu category {CategoryId}", menuInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Updates an existing navigation sub-category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult updateSubCatResult = await _menuService.UpdateSubCategoryAsync(new SubCategoryRequest
            {
                Id = menuInputViewModel.Id,
                CategoryId = menuInputViewModel.CategoryId,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);
            if (!updateSubCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully updated the subCategory"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update menu sub-category {SubCategoryId}", menuInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Delete

    /// <summary>Deletes a navigation category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult deleteCatResult = await _menuService.DeleteCategoryAsync(new CategoryRequest
            {
                Id = menuInputViewModel.Id,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Controller = menuInputViewModel.Controller,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);
            if (!deleteCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully deleted the category"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete menu category {CategoryId}", menuInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    /// <summary>Deletes a navigation sub-category.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            ServiceResult deleteSubCatResult = await _menuService.DeleteSubCategoryAsync(new SubCategoryRequest
            {
                Id = menuInputViewModel.Id,
                CategoryId = menuInputViewModel.CategoryId,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            }, HttpContext.RequestAborted);

            if (!deleteSubCatResult.Success)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            await _cache.InvalidateNavigationMenuCacheAsync();
            return Json(new { result = true, message = _localizer["Successfully deleted the subCategory"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete menu sub-category {SubCategoryId}", menuInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Excel

    /// <summary>Exports the navigation menu (categories and sub-categories) to an Excel file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelMenu(string fileName = "")
    {
        var account = _sessionService.GetAccount()!;
        var categories = await _menuService.GetAllCategoriesAsync(HttpContext.RequestAborted);
        var subCats = await _menuService.GetAllSubCategoriesAsync(HttpContext.RequestAborted);
        var stream = _excelExportService.CreateMenuExcel(categories, subCats, _localize);
        string name = fileName.ToExcelFileName(account.TimeZoneIanaId!);
        return File(stream, "application/octet-stream", name);
    }

    #endregion

    #endregion

    #region PrivateNote

    #region Create

    #region Write

    #region PrivateNoteBoard

    /// <summary>Creates or updates a private note board post.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WritePrivateNoteBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(boardInputViewModel.Locked));

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            if (loggedInAccount.Role != Role.Admin && loggedInAccount.Role != Role.User)
                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });

            BoardRequest boardRequest = new()
            {
                Type = EnumHelper.GetDescription(BoardType.PrivateNote),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Writer = loggedInAccount.Nickname!,
                Noticed = loggedInAccount.Role == Role.Admin && boardInputViewModel.Noticed
            };

            AttachedFileDto? attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using MemoryStream ms = new();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileDto
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            ServiceResult result = await _boardService.WriteBoardAsync(boardRequest, attachedFile, HttpContext.RequestAborted);
            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully created."].Value })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write private note board {Title}", boardInputViewModel.Title);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region PrivateNoteComment

    /// <summary>Submits a new PrivateNoteComment.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WritePrivateNoteComment(
        [FromBody] BoardCommentInputViewModel boardCommentInputViewModel)
    {
        try
        {
            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                return Json(new { result = false, error = _localizer["Please enter a comment."].Value });

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to write comment."].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            BoardCommentRequest commentRequest = new()
            {
                BoardId = boardCommentInputViewModel.BoardId,
                AvatarImagePath = loggedInAccount.AvatarImagePath!,
                Writer = loggedInAccount.Nickname!,
                Content = boardCommentInputViewModel.Content
            };

            ServiceResult result = await _boardService.WriteCommentAsync(commentRequest, loggedInAccount.Nickname!, requiredType: EnumHelper.GetDescription(BoardType.PrivateNote), ct: HttpContext.RequestAborted);
            return result.Success
                ? Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write private note comment on board {BoardId}", boardCommentInputViewModel.BoardId);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #region Summernote Image File Upload
    /// <summary>Uploads a ImageFile file.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile? summernoteImageFile)
    {
        if (summernoteImageFile == null)
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > _serverSettings.Value.MaxAttachedFileSizeBytes)
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than {0}MB.", _serverSettings.Value.MaxAttachedFileSizeBytes / (1024 * 1024)].Value });

        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });

        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            await summernoteImageFile.CopyToAsync(ms);
            bytes = ms.ToArray();
        }

        var file = new AttachedFileDto
        {
            Bytes = bytes,
            ContentType = summernoteImageFile.ContentType,
            FileName = summernoteImageFile.FileName,
            Size = summernoteImageFile.Length
        };

        SummernoteUploadResult uploadResult = await _boardService.UploadSummernoteImageAsync(file, "Management", EnumHelper.GetDescription(BoardType.PrivateNote), HttpContext.RequestAborted);

        if (!uploadResult.Success)
            return Ok(new { result = false, errorMessage = _localizer[uploadResult.ErrorKey ?? "Input is invalid"].Value });

        return Ok(new
        {
            result = true,
            file = File(uploadResult.FileBytes!, uploadResult.ContentType!, uploadResult.FileName!),
            filePath = _rsa.Encrypt(uploadResult.FilePath!)
        });
    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write

    /// <summary>Displays the private note (PrivateNote board) page.</summary>
    public async Task<IActionResult> PrivateNote(string method = "list", int? boardId = null, int page = 1,
        string searchType = "", string searchText = "")
    {
        switch (method)
        {
            // Write
            case "write":
            {
                PrivateNoteOutputViewModel privateNoteOutputViewModel = new() { Method = method };
                bool isAccountSessionExist = _sessionService.GetAccount() != null; // Check if account session exists after login
                return isAccountSessionExist ? View(privateNoteOutputViewModel) : RedirectToAction("Login", "Account");
            }
            // Detail view
            case "detail":
            {
                AccountResponse loggedInAccount = _sessionService.GetAccount()!; // Get Session

                if (boardId == null)
                {
                    return RedirectToAction(nameof(BoardType.PrivateNote), "Management");
                }

                PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
                {
                    Method = method,
                    DetailCurrentPage = page,
                    DetailBoardId = (int)boardId
                };

                BoardResponse? privateNoteBoard = await _boardService.GetBoardByIdAsync((long)boardId, HttpContext.RequestAborted);
                BoardAttachedFileDto? attachedFile = await _boardService.GetAttachedFileByBoardIdAsync((long)boardId, HttpContext.RequestAborted);

                try
                {
                    if (privateNoteBoard == null || privateNoteBoard.Deleted || !_boardService.CanView(privateNoteBoard, loggedInAccount))
                        return RedirectToAction(nameof(BoardType.PrivateNote), "Management");

                    await _boardService.IncrementBoardViewAsync((long)boardId, HttpContext.RequestAborted);

                    (privateNoteBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(privateNoteBoard.Content ?? "", HttpContext.RequestAborted);

                    privateNoteOutputViewModel.BoardOutputViewModel =
                        new BoardOutputViewModel
                        {
                            Title = privateNoteBoard.Title,
                            Writer = privateNoteBoard.Writer,
                            Views = privateNoteBoard.View,
                            Content = privateNoteBoard.Content,
                            Updated = privateNoteBoard.Updated,
                            BoardAttachedFileName = attachedFile?.Name ?? "",
                            BoardAttachedFileExtension = attachedFile?.Extension ?? "",
                            BoardAttachedFileSize = attachedFile?.Size ?? 0,
                            BoardAttachedFilePath = attachedFile?.Path ?? "",
                            IsImgTagIncluded = isImgTagIncluded,
                            CanModify = loggedInAccount.IsOwner(privateNoteBoard.Writer),
                            // Private notes are single-owner: unlike the free forum, an admin may not
                            // delete another account's private note (server enforces this in DeleteBoard).
                            CanDelete = loggedInAccount.IsOwner(privateNoteBoard.Writer)
                        };

                    if (string.IsNullOrEmpty(privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                    {
                        return View(privateNoteOutputViewModel);
                    }

                    byte[]? fileData = await _boardService.DownloadFileAsync(
                        privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", HttpContext.RequestAborted);

                    if (fileData != null)
                    {
                        privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFileContentType = "application/octet-stream";
                    }

                    // Preload comments and accounts for detail view
                    privateNoteOutputViewModel.DetailBoardComments =
                    [
                        .. await _boardService.GetCommentsByBoardIdAsync((long)boardId, HttpContext.RequestAborted)
                    ];
                    privateNoteOutputViewModel.AllAccounts = await _accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
                    privateNoteOutputViewModel.AdminNicknames = privateNoteOutputViewModel.AllAccounts.ToAdminNicknameSet();
                    privateNoteOutputViewModel.LoggedInAccount = loggedInAccount;
                    privateNoteOutputViewModel.LoggedInAccountTimeZoneIanaId = loggedInAccount.TimeZoneIanaId ?? "";

                    return View(privateNoteOutputViewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load private note board detail for board {BoardId}", boardId);
                    return RedirectToAction(nameof(BoardType.PrivateNote), "Management");
                }
            }
            // Edit
            case "edit" when boardId == null:
                return RedirectToAction(nameof(BoardType.PrivateNote), "Management");
            case "edit":
            {
                PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
                {
                    Method = method,
                    EditCurrentPage = page,
                    EditBoardId = (int)boardId
                };

                AccountResponse loggedInAccount = _sessionService.GetAccount()!;

                BoardResponse? privateNoteBoard = await _boardService.GetBoardByIdAsync((long)boardId, HttpContext.RequestAborted);
                BoardAttachedFileDto? attachedFile = await _boardService.GetAttachedFileByBoardIdAsync((long)boardId, HttpContext.RequestAborted);

                try
                {
                    if (privateNoteBoard == null || privateNoteBoard.Deleted || !loggedInAccount.IsOwner(privateNoteBoard.Writer))
                    {
                        return RedirectToAction(nameof(BoardType.PrivateNote), "Management");
                    }

                    (privateNoteBoard.Content, bool isImgTagIncluded) = await _boardService.PrepareHtmlForDisplayAsync(privateNoteBoard.Content ?? "", HttpContext.RequestAborted);

                    privateNoteOutputViewModel.BoardOutputViewModel =
                        new BoardOutputViewModel
                        {
                            Title = privateNoteBoard.Title,
                            Writer = privateNoteBoard.Writer,
                            Views = privateNoteBoard.View,
                            Content = privateNoteBoard.Content,
                            Locked = privateNoteBoard.Locked,
                            Updated = privateNoteBoard.Updated,
                            BoardAttachedFileName = attachedFile?.Name ?? "",
                            BoardAttachedFileExtension = attachedFile?.Extension ?? "",
                            BoardAttachedFileSize = attachedFile?.Size ?? 0,
                            BoardAttachedFilePath = attachedFile?.Path ?? "",
                            IsImgTagIncluded = isImgTagIncluded
                        };

                    if (string.IsNullOrEmpty(privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? ""))
                    {
                        return View(privateNoteOutputViewModel);
                    }

                    byte[]? fileData = await _boardService.DownloadFileAsync(
                        privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "", HttpContext.RequestAborted);

                    if (fileData == null)
                    {
                        return View(privateNoteOutputViewModel);
                    }

                    privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFileBase64Data = Convert.ToBase64String(fileData);
                    privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFileContentType = "application/octet-stream";

                    return View(privateNoteOutputViewModel);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load private note board edit form for board {BoardId}", boardId);
                    return RedirectToAction(nameof(BoardType.PrivateNote), "Management");
                }
            }
            // List
            default:
            {
                bool isAccountSessionExist = _sessionService.GetAccount() != null;
                AccountResponse loggedInAccount = _sessionService.GetAccount()!;

                #region Board default paging logic

                const int pageSize = 5;
                if (page < 1)
                {
                    page = 1;
                }

                #endregion

                #region Logic to get all related board data

                var privateNoteBoards =
                    (await _boardService.GetBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote), HttpContext.RequestAborted)).ToList();
                privateNoteBoards =
                [
                    .. privateNoteBoards.Where(m => m.Writer == loggedInAccount.Nickname)
                        .Where(m => !m.Noticed).OrderByDescending(a => a.Id)
                ];

                #endregion

                #region Search keyword logic

                switch (searchType)
                {
                    case "Title" when !string.IsNullOrEmpty(searchText):
                    {
                        if (isAccountSessionExist) // If logged in
                        {
                            switch (loggedInAccount.Role)
                            {
                                // Administrator can see all posts
                                case Role.Admin:
                                    privateNoteBoards =
                                        [.. privateNoteBoards.Where(a => a.Title!.Contains(searchText))];
                                    break;
                                // User can see regular posts + their own private posts
                                case Role.User:
                                {
                                    List<BoardResponse> searchPrivateNoteBoards = [];
                                    List<BoardResponse> noLockedPrivateNoteBoards =
                                    [
                                        .. privateNoteBoards
                                            .Where(a => a.Title!.Contains(searchText)).Where(a => !a.Locked)
                                    ];
                                    List<BoardResponse> myLockedPrivateNoteBoards =
                                    [
                                        .. privateNoteBoards
                                            .Where(a => a.Title!.Contains(searchText))
                                            .Where(a => a.Locked && a.Writer == loggedInAccount.Nickname)
                                    ];
                                    searchPrivateNoteBoards.AddRange(noLockedPrivateNoteBoards);
                                    searchPrivateNoteBoards.AddRange(myLockedPrivateNoteBoards);

                                    privateNoteBoards = searchPrivateNoteBoards;
                                    break;
                                }
                                // If logged in but has neither Admin nor User role, it's a bug
                            }
                        }
                        else // If not logged in
                        {
                            privateNoteBoards =
                            [
                                .. privateNoteBoards.Where(a => a.Title!.Contains(searchText))
                                    .Where(a => !a.Locked)
                            ]; // When not logged in, private posts should not be searchable
                        }

                        break;
                    }
                    case "Writer" when !string.IsNullOrEmpty(searchText):
                        privateNoteBoards = [.. privateNoteBoards.Where(a => a.Writer!.Contains(searchText))];
                        break;
                }

                #endregion

                PrivateNoteOutputViewModel privateNoteOutputViewModel = new();

                #region Notice

                privateNoteOutputViewModel.NoticeBoards = [];

                #endregion

                #region Board list

                privateNoteOutputViewModel.Method = "list";

                #endregion

                #region Board paging logic

                privateNoteOutputViewModel.Pager = new Pager(privateNoteBoards.Count, page, pageSize);

                #endregion

                #region Board data logic

                privateNoteOutputViewModel.Boards =
                [
                    .. privateNoteBoards.Skip((page - 1) * pageSize)
                        .Take(privateNoteOutputViewModel.Pager.PageSize)
                ];

                #endregion

                #region Preload comment counts and attached files for view
                privateNoteOutputViewModel.LoggedInAccount = loggedInAccount;
                privateNoteOutputViewModel.LoggedInAccountTimeZoneIanaId = loggedInAccount.TimeZoneIanaId ?? "";
                privateNoteOutputViewModel.AllAccounts = await _accountService.GetAllAccountsAsync(HttpContext.RequestAborted);
                privateNoteOutputViewModel.AdminNicknames = privateNoteOutputViewModel.AllAccounts.ToAdminNicknameSet();

                var boardResponses = privateNoteOutputViewModel.Boards.ToList();
                List<long> boardIds = [.. boardResponses.Select(b => b.Id)];
                Dictionary<long, int> commentCounts = await _boardService.GetCommentCountsForBoardsAsync(boardIds, HttpContext.RequestAborted);
                Dictionary<long, BoardAttachedFileDto?> attachedFiles = await _boardService.GetAttachedFilesForBoardsAsync(boardIds, HttpContext.RequestAborted);

                foreach (var board in boardResponses)
                {
                    privateNoteOutputViewModel.CommentCountByBoardId[board.Id] = commentCounts.GetValueOrDefault(board.Id, 0);
                    BoardAttachedFileDto? af = attachedFiles.GetValueOrDefault(board.Id);
                    privateNoteOutputViewModel.AttachedFilesByBoardId[board.Id] = af != null && !string.IsNullOrEmpty(af.Name) ? [af] : [];
                }
                #endregion

                privateNoteOutputViewModel.SelectedSearchType = searchType;
                privateNoteOutputViewModel.TypedSearchText = searchText;
                return View(privateNoteOutputViewModel);
            }
        }
    }

    #endregion

    #region IsBoardExists

    /// <summary>Checks whether a Board record exists.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsBoardExists(int id)
    {
        try
        {
            string? nickname = _sessionService.GetAccount()!.Nickname;
            BoardResponse? board = await _boardService.GetBoardByIdAsync(id, HttpContext.RequestAborted);

            return board == null || board.Deleted || board.Writer != nickname
                   || board.Type != EnumHelper.GetDescription(BoardType.PrivateNote)
                ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                : Json(new { result = true, PrivateNoteBoard = board });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check private note board existence for id {BoardId}", id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #region Update

    #region Edit

    /// <summary>Saves edits to an existing PrivateNoteBoard.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> EditPrivateNoteBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(boardInputViewModel.Locked));

            if (!ModelState.IsValid)
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });

            boardInputViewModel.Content ??= "";

            bool isAccountSessionExist = _sessionService.GetAccount() != null;
            if (!isAccountSessionExist)
                return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });

            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            BoardRequest boardRequest = new()
            {
                Id = boardInputViewModel.Id,
                Type = EnumHelper.GetDescription(BoardType.PrivateNote),
                Title = boardInputViewModel.Title,
                Content = boardInputViewModel.Content,
                Locked = boardInputViewModel.Locked
            };

            AttachedFileDto? attachedFile = null;
            if (boardInputViewModel.UploadedFile != null)
            {
                using MemoryStream ms = new();
                await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                attachedFile = new AttachedFileDto
                {
                    Bytes = ms.ToArray(),
                    ContentType = boardInputViewModel.UploadedFile.ContentType,
                    FileName = boardInputViewModel.UploadedFile.FileName,
                    Size = boardInputViewModel.UploadedFile.Length
                };
            }

            ServiceResult result = await _boardService.EditBoardAsync(boardRequest, loggedInAccount.Nickname!, attachedFile, HttpContext.RequestAborted);
            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully updated."].Value })
                : Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit private note board {BoardId}", boardInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #region Delete

    #region Board

    /// <summary>Deletes an existing Board record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel)
    {
        try
        {
            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            // Private notes are single-owner: unlike the free forum, an admin may not delete
            // another account's private note board.
            ServiceResult result = await _boardService.DeleteBoardAsync(
                boardInputViewModel.Id,
                EnumHelper.GetDescription(BoardType.PrivateNote),
                loggedInAccount.Nickname!,
                false,
                HttpContext.RequestAborted);

            return result.Success
                ? Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete private note board {BoardId}", boardInputViewModel.Id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region BoardComment

    /// <summary>Deletes an existing Comment record.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            AccountResponse loggedInAccount = _sessionService.GetAccount()!;

            // Private notes are single-owner: unlike the free forum, an admin may not delete
            // another account's private note comments.
            ServiceResult result = await _boardService.DeleteCommentAsync(
                id,
                loggedInAccount.Nickname!,
                false,
                EnumHelper.GetDescription(BoardType.PrivateNote),
                HttpContext.RequestAborted);

            return result.Success
                ? Json(new { result = true })
                : Json(new { result = false, error = result.ErrorKey });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete private note comment {CommentId}", id);
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion
}
