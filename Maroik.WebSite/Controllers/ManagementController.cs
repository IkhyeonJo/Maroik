using Mapster;
﻿using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.WebSite.Extensions;
using Maroik.WebSite.Utilities;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Management;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers;

public class ManagementController : BaseController
{
    private readonly RSA _rsa;
    private readonly IHtmlLocalizer<ManagementController> _localizer;
    private readonly ILogger<ManagementController> _logger;
    private readonly IAccountRepository _accountRepository;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISubCategoryRepository _subCategoryRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
    private readonly IBoardCommentRepository _boardCommentRepository;
    private readonly IFileClient _fileClient;
    private readonly IClamavClient _clamavClient;
    private readonly IPasswordService _passwordService;
    private readonly ApplicationDbContext _context;

    public ManagementController(IHtmlLocalizer<ManagementController> localizer,
        ILogger<ManagementController> logger, IBoardRepository boardRepository,
        IBoardAttachedFileRepository boardAttachedFileRepository, IBoardCommentRepository boardCommentRepository,
        IAccountRepository accountRepository, IHostEnvironment hostEnvironment,
        ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository,
        IFileClient fileClient, IClamavClient clamavClient,
        ApplicationDbContext context, IPasswordService passwordService)
    {
        _rsa = new RSA(ServerSetting.RSAAlgorithm, Encoding.UTF8, ServerSetting.RSAPrivateKey, ServerSetting.RSAPublicKey);
        _localizer = localizer;
        _logger = logger;
        _boardRepository = boardRepository;
        _boardAttachedFileRepository = boardAttachedFileRepository;
        _accountRepository = accountRepository;
        _hostEnvironment = hostEnvironment;
        _categoryRepository = categoryRepository;
        _subCategoryRepository = subCategoryRepository;
        _boardCommentRepository = boardCommentRepository;
        _fileClient = fileClient;
        _clamavClient = clamavClient;
        _context = context;
        _passwordService = passwordService;
    }

    #region Profile

    #region Read

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;
        AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

        return View(new ProfileOutputViewModel()
        {
            Email = tempAccount.Email, AvatarImagePath = tempAccount.AvatarImagePath,
            Nickname = tempAccount.Nickname,
            Created = tempAccount.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
            TimeZoneIanaId = tempAccount.TimeZoneIanaId
        });
    }

    #endregion

    #region Update

    #region ProfileAvatar

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

        if (file.Length <= 0 || file.Length > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed"].Value });
        }

        // Copy memory stream before ClamAV scan
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        if (!await _clamavClient.ScanWithClamavAsync(memoryStream, ServerSetting.ClamavHost, ServerSetting.ClamavPort))
        {
            return BadRequest("File may be infected with a virus.");
        }

        // Validate image (using Magick.NET, including SVG blocking)
        memoryStream.Position = 0;
        try
        {
            using var image = new MagickImage(memoryStream);
            if (image.Format == MagickFormat.Svg)
            {
                return Ok(new { result = false, errorMessage = _localizer["SVG format is not allowed"].Value });
            }
        }
        catch
        {
            return Ok(new { result = false, errorMessage = _localizer["Invalid image file."].Value });
        }

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account),
            out byte[] resultByte);

        string avatarPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", "Avatar");
        if (!Directory.Exists(avatarPath))
        {
            Directory.CreateDirectory(avatarPath);
        }

        string guid = Guid.NewGuid().ToString().ToUpper();
        string avatarFile = $"{guid}{extension}";
        string filePath = Path.Combine(avatarPath, avatarFile);

    #pragma warning disable SCS0018
        await using (var stream = new FileStream(filePath, FileMode.Create))
    #pragma warning restore SCS0018
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        // Remove execute permission (chmod 644)
        var chmod = new ProcessStartInfo
        {
            FileName = "/bin/chmod",
#pragma warning disable SCS0001
            Arguments = $"644 \"{filePath}\"",
#pragma warning restore SCS0001
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using (var process = Process.Start(chmod))
        {
            await process?.WaitForExitAsync()!;
        }

        try
        {
            AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            tempAccount.AvatarImagePath = $"/upload/Management/Profile/Avatar/{avatarFile}";
            await _accountRepository.UpdateAccountAsync(tempAccount.Adapt<AccountRequest>());

            return Ok(new { result = true });
        }
        catch
        {
            return Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region ProfileTimeZone

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateProfileTimeZone(ProfileInputViewModel profileInputViewModel)
    {
        _ = ModelState.Remove(nameof(profileInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(profileInputViewModel.Password));
        _ = ModelState.Remove(nameof(profileInputViewModel.NewPassword));

        if (ModelState.IsValid)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);
                AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);
                tempAccount.TimeZoneIanaId = profileInputViewModel.TimeZoneIanaId;
                await _accountRepository.UpdateAccountAsync(tempAccount.Adapt<AccountRequest>());

                HttpContext.Session.Remove(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account));
                tempAccount.HashedPassword = null;
                HttpContext.Session.Set(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account),
                    Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));
            }
            catch
            {
                Console.WriteLine(_localizer["Input is invalid"].Value);
            }
        }

        return RedirectToAction("Profile", "Management");
    }

    #endregion

    #region ProfilePassword

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UpdateProfilePassword([FromBody] ProfileInputViewModel profileInputViewModel)
    {
        _ = ModelState.Remove(nameof(profileInputViewModel.Nickname));
        _ = ModelState.Remove(nameof(profileInputViewModel.TimeZoneIanaId));
        
        if (ModelState.IsValid)
        {
            try
            {
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);
                AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

                if (_passwordService.VerifyPassword(profileInputViewModel.Password ?? "", tempAccount.HashedPassword)) // right password
                {
                    tempAccount.HashedPassword = _passwordService.HashPassword(profileInputViewModel.NewPassword);
                    await _accountRepository.UpdateAccountAsync(tempAccount.Adapt<AccountRequest>());
                    return Json(new
                        { result = true, message = _localizer["Password change successfully done."].Value });
                }
                else // wrong password
                {
                    return Json(new
                        { result = false, error = _localizer["Invalid password. Please check again."].Value });
                }
            }
            catch
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        else
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion

    #region Account

    #region Create

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

            if (ModelState.IsValid)
            {
                #region Email format check

                static bool IsValidEmail(string email)
                {
                    if (!MailAddress.TryCreate(email, out MailAddress mailAddress))
                    {
                        return false;
                    }

                    // And if you want to be more strict:
                    string[] hostParts = mailAddress.Host.Split('.');
                    if (hostParts.Length == 1)
                    {
                        return false; // No dot.
                    }

                    if (hostParts.Any(p => p == string.Empty))
                    {
                        return false; // Double dot.
                    }

                    if (hostParts[^1].Length < 2)
                    {
                        return false; // TLD only one letter.
                    }

                    if (mailAddress.User.Contains(' '))
                    {
                        return false;
                    }

                    if (mailAddress.User.Split('.').Any(p => p == string.Empty))
                    {
                        return false; // Double dot or dot at end of user part.
                    }

                    return true;
                }

                if (!IsValidEmail(accountInputViewModel.Email)) // Invalid email format
                {
                    return Json(new { result = false, error = _localizer["Invalid email form"].Value });
                }

                #endregion

                #region Password format check

                if (!new Regex(
                            "^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$")
                        .IsMatch(accountInputViewModel.Password)) // Invalid password format
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
                else if (accountInputViewModel.Nickname.Length is not (>= 1 and <= 255))
                {
                    return Json(new
                    {
                        result = false, error = _localizer["Nickname is must be between 1 and 255 characters"].Value
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

                #region Check if account exists with the entered email

                if (await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email) != null)
                {
                    return Json(new
                        { result = false, error = _localizer["This account has already been created."].Value });
                }

                #endregion

                #region Create account

                await _accountRepository.CreateAccountAsync
                (
                    new AccountRequest()
                    {
                        Email = accountInputViewModel.Email,
                        HashedPassword = _passwordService.HashPassword(accountInputViewModel.Password),
                        Nickname = accountInputViewModel.Nickname,
                        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                        Role = accountInputViewModel.Role,
                        TimeZoneIanaId = accountInputViewModel.TimeZoneIanaId,
                        Locked = false,
                        LoginAttempt = 0,
                        EmailConfirmed = true,
                        AgreedServiceTerms = true,
                        RegistrationToken = null,
                        ResetPasswordToken = null,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        Message = EnumHelper.GetDescription(AccountMessage.Success),
                        Deleted = false
                    }
                );
                return Json(new { result = true, message = _localizer["Account create successfully done."].Value });

                #endregion
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
    public async Task<IActionResult> Account(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<AccountOutputViewModel> accountOutputViewModels = [];
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);

            string loginedAccountTimeZoneIanaId = JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            #region Accounts

            foreach (AccountResponse item in await _accountRepository.GetAllAsync())
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    accountOutputViewModels.Add(new AccountOutputViewModel()
                    {
                        Email = item.Email,
                        HashedPassword = item.HashedPassword,
                        Nickname = item.Nickname,
                        AvatarImagePath = item.AvatarImagePath,
                        Role = item.Role,
                        TimeZoneIanaId = item.TimeZoneIanaId,
                        Locked = item.Locked,
                        LoginAttempt = item.LoginAttempt,
                        EmailConfirmed = item.EmailConfirmed,
                        AgreedServiceTerms = item.AgreedServiceTerms,
                        RegistrationToken = item.RegistrationToken,
                        ResetPasswordToken = item.ResetPasswordToken,
                        Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                        Message = item.Message,
                        Deleted = item.Deleted
                    });
                }
                else
                {
                    if (item.Email.ToString().Contains(wholeSearch) ||
                        (item.HashedPassword?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Nickname?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.AvatarImagePath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.TimeZoneIanaId?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Locked.ToString() ?? "").Contains(wholeSearch) ||
                        (item.LoginAttempt.ToString() ?? "").Contains(wholeSearch) ||
                        (item.EmailConfirmed.ToString() ?? "").Contains(wholeSearch) ||
                        (item.AgreedServiceTerms.ToString() ?? "").Contains(wholeSearch) ||
                        (item.RegistrationToken?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.ResetPasswordToken?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "")
                        .Contains(wholeSearch) ||
                        (item.Message?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Deleted.ToString() ?? "").Contains(wholeSearch))
                    {
                        accountOutputViewModels.Add(new AccountOutputViewModel()
                        {
                            Email = item.Email,
                            HashedPassword = item.HashedPassword,
                            Nickname = item.Nickname,
                            AvatarImagePath = item.AvatarImagePath,
                            Role = item.Role,
                            TimeZoneIanaId = item.TimeZoneIanaId,
                            Locked = item.Locked,
                            LoginAttempt = item.LoginAttempt,
                            EmailConfirmed = item.EmailConfirmed,
                            AgreedServiceTerms = item.AgreedServiceTerms,
                            RegistrationToken = item.RegistrationToken,
                            ResetPasswordToken = item.ResetPasswordToken,
                            Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                            Message = item.Message,
                            Deleted = item.Deleted
                        });
                    }
                }
            }

            #endregion

            IQueryable<AccountOutputViewModel> result = accountOutputViewModels.OrderBy(m => m.Email)
                .ThenBy(m => m.Nickname).ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated)
                .AsQueryable();
            return PartialView("_AccountGrid", result);
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsAccountExists(string email)
    {
        try
        {
            AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(email);

            return tempAccount == null
                ? Json(new
                {
                    result = false, error = _localizer["Fail to find the account by given email address"].Value
                })
                : (IActionResult)Json(new { result = true, account = tempAccount });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(accountInputViewModel.Nickname));
            _ = ModelState.Remove(nameof(accountInputViewModel.AvatarImagePath));
            _ = ModelState.Remove(nameof(accountInputViewModel.RegistrationToken));
            _ = ModelState.Remove(nameof(accountInputViewModel.ResetPasswordToken));

            if (ModelState.IsValid)
            {
                AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

                if (tempAccount == null)
                {
                    return Json(new { result = false, error = _localizer["Email address is wrong"].Value });
                }

                tempAccount.Email = accountInputViewModel.Email;
                tempAccount.HashedPassword = _passwordService.HashPassword(accountInputViewModel.Password);
                tempAccount.Role = accountInputViewModel.Role;
                tempAccount.TimeZoneIanaId = accountInputViewModel.TimeZoneIanaId;
                tempAccount.Locked = accountInputViewModel.Locked;
                if (accountInputViewModel.Locked == false)
                {
                    tempAccount.LoginAttempt = 0;
                }

                tempAccount.EmailConfirmed = accountInputViewModel.EmailConfirmed;
                tempAccount.AgreedServiceTerms = accountInputViewModel.AgreedServiceTerms;
                tempAccount.Updated = DateTime.UtcNow;
                tempAccount.Message = accountInputViewModel.Message;
                tempAccount.Deleted = accountInputViewModel.Deleted;

                await _accountRepository.UpdateAccountAsync(tempAccount.Adapt<AccountRequest>());

                return Json(new { result = true, message = _localizer["Successfully updated the account"].Value });
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

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            AccountResponse tempAccount = await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

            if (tempAccount == null)
            {
                return Json(new
                {
                    result = false, error = _localizer["Fail to find the account by given email address"].Value
                });
            }
            else
            {
                tempAccount.Deleted = true;
                await _accountRepository.UpdateAccountAsync(tempAccount.Adapt<AccountRequest>());
                return Json(new { result = true, message = _localizer["Successfully deleted the account"].Value });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelAccount(string fileName = "")
    {
        await Task.Yield();

        List<AccountOutputViewModel> accountOutputViewModels = new();
        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

        #region Accounts

        foreach (AccountResponse item in await _accountRepository.GetAllAsync())
        {
            accountOutputViewModels.Add(new AccountOutputViewModel()
            {
                Email = item.Email,
                HashedPassword = item.HashedPassword,
                Nickname = item.Nickname,
                AvatarImagePath = item.AvatarImagePath,
                Role = item.Role,
                TimeZoneIanaId = item.TimeZoneIanaId,
                Locked = item.Locked,
                LoginAttempt = item.LoginAttempt,
                EmailConfirmed = item.EmailConfirmed,
                AgreedServiceTerms = item.AgreedServiceTerms,
                RegistrationToken = item.RegistrationToken,
                ResetPasswordToken = item.ResetPasswordToken,
                Created = item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Updated = item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId),
                Message = item.Message,
                Deleted = item.Deleted
            });
        }

        accountOutputViewModels = accountOutputViewModels.OrderBy(m => m.Email).ThenBy(m => m.Nickname)
            .ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (SpreadsheetDocument document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = document.WorkbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            {
                Id = document.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

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
                CreateCell(_localizer[nameof(AccountOutputViewModel.Email)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.HashedPassword)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Nickname)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.AvatarImagePath)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Role)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.TimeZoneIanaId)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Locked)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.LoginAttempt)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.EmailConfirmed)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.AgreedServiceTerms)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.RegistrationToken)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.ResetPasswordToken)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Created)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Updated)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Message)].Value),
                CreateCell(_localizer[nameof(AccountOutputViewModel.Deleted)].Value)
            );
            sheetData.Append(headerRow);

            int recordIndex = 2;
            foreach (AccountOutputViewModel item in accountOutputViewModels)
            {
                Row recordRow = new Row()
                {
                    RowIndex = (uint)recordIndex
                };

                recordRow.Append(
                    CreateCell(item.Email),
                    CreateCell(item.HashedPassword),
                    CreateCell(item.Nickname),
                    CreateCell(item.AvatarImagePath),
                    CreateCell(item.Role),
                    CreateCell(item.TimeZoneIanaId),
                    CreateCell(item.Locked.ToString()),
                    CreateCell(item.LoginAttempt.ToString()),
                    CreateCell(item.EmailConfirmed.ToString()),
                    CreateCell(item.AgreedServiceTerms.ToString()),
                    CreateCell(item.RegistrationToken),
                    CreateCell(item.ResetPasswordToken),
                    CreateCell(item.Created.ToString()),
                    CreateCell(item.Updated.ToString()),
                    CreateCell(item.Message),
                    CreateCell(item.Deleted.ToString())
                );
                sheetData.Append(recordRow);

                recordIndex++;
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

    #region Menu

    #region Create

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> CreateCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (ModelState.IsValid)
            {
                CategoryResponse category = new()
                {
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Controller = menuInputViewModel.Controller,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _categoryRepository.CreateCategoryAsync(category.Adapt<CategoryRequest>());

                return Json(new { result = true, message = _localizer["Successfully created the category"].Value });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> CreateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (ModelState.IsValid)
            {
                SubCategoryResponse subCategory = new()
                {
                    CategoryId = menuInputViewModel.CategoryId,
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _subCategoryRepository.CreateSubCategoryAsync(subCategory.Adapt<SubCategoryRequest>());

                return Json(new
                    { result = true, message = _localizer["Successfully created the subCategory"].Value });
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
    public async Task<IActionResult> Menu(string wholeSearch)
    {
        if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
        {
            List<MenuOutputViewModel> menuOutputViewModels = [];

            #region AdminCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Controller?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            #region UserCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Controller?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            #region AnonymousCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Controller?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            #region AdminSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        item.CategoryId.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            #region UserSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        item.CategoryId.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            #region AnonymousSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
            {
                if (string.IsNullOrEmpty(wholeSearch))
                {
                    menuOutputViewModels.Add(new MenuOutputViewModel()
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
                    });
                }
                else
                {
                    if (item.Id.ToString().Contains(wholeSearch) ||
                        item.CategoryId.ToString().Contains(wholeSearch) ||
                        (item.Name?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.DisplayName?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.IconPath?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Action?.ToString() ?? "").Contains(wholeSearch) ||
                        (item.Role?.ToString() ?? "").Contains(wholeSearch) ||
                        item.Order.ToString().Contains(wholeSearch))
                    {
                        menuOutputViewModels.Add(new MenuOutputViewModel()
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
                        });
                    }
                }
            }

            #endregion

            IQueryable<MenuOutputViewModel> result = menuOutputViewModels.OrderByDescending(m => m.Id)
                .ThenByDescending(m => m.CategoryId).AsQueryable();
            return PartialView("_MenuGrid", result);
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsCategoryExists(int id)
    {
        try
        {
            List<MenuOutputViewModel> menuOutputViewModels = [];

            #region AdminCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            #region UserCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            #region AnonymousCategories

            foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            MenuOutputViewModel selectedCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault();
            if (selectedCategory == null)
            {
                throw new Exception(); // Not found in DB
            }

            return Json(new
                { result = true, category = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsSubCategoryExists(int id)
    {
        try
        {
            List<MenuOutputViewModel> menuOutputViewModels = [];

            #region AdminSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            #region UserSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            #region AnonymousSubCategories

            foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
            {
                menuOutputViewModels.Add(new MenuOutputViewModel()
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
                });
            }

            #endregion

            MenuOutputViewModel selectedCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault();
            if (selectedCategory == null)
            {
                throw new Exception(); // Not found in DB
            }

            return Json(new
                { result = true, subCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (ModelState.IsValid)
            {
                CategoryResponse category = new()
                {
                    Id = menuInputViewModel.Id,
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Controller = menuInputViewModel.Controller,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _categoryRepository.UpdateCategoryAsync(category.Adapt<CategoryRequest>());

                return Json(new { result = true, message = _localizer["Successfully updated the category"].Value });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> UpdateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (ModelState.IsValid)
            {
                SubCategoryResponse subCategory = new()
                {
                    Id = menuInputViewModel.Id,
                    CategoryId = menuInputViewModel.CategoryId,
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _subCategoryRepository.UpdateSubCategoryAsync(subCategory.Adapt<SubCategoryRequest>());

                return Json(new
                    { result = true, message = _localizer["Successfully updated the subCategory"].Value });
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

    #region Delete

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (ModelState.IsValid)
            {
                CategoryResponse category = new()
                {
                    Id = menuInputViewModel.Id,
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Controller = menuInputViewModel.Controller,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _categoryRepository.DeleteCategoryAsync(category.Adapt<CategoryRequest>());

                return Json(new { result = true, message = _localizer["Successfully deleted the category"].Value });
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

            if (ModelState.IsValid)
            {
                SubCategoryResponse subCategory = new()
                {
                    Id = menuInputViewModel.Id,
                    CategoryId = menuInputViewModel.CategoryId,
                    Name = menuInputViewModel.Name,
                    DisplayName = menuInputViewModel.DisplayName,
                    IconPath = menuInputViewModel.IconPath,
                    Action = menuInputViewModel.Action,
                    Role = menuInputViewModel.Role,
                    Order = menuInputViewModel.Order
                };
                await _subCategoryRepository.DeleteSubCategoryAsync(subCategory.Adapt<SubCategoryRequest>());

                return Json(new
                    { result = true, message = _localizer["Successfully deleted the subCategory"].Value });
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

    #region Excel

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelMenu(string fileName = "")
    {
        await Task.Yield();

        List<MenuOutputViewModel> menuOutputViewModels = [];
        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities
                .Session.Account), out byte[] resultByte);
        string loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

        #region AdminCategories
        foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        #region UserCategories
        foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        #region AnonymousCategories
        foreach (CategoryResponse item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        #region AdminSubCategories
        foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        #region UserSubCategories
        foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        #region AnonymousSubCategories
        foreach (SubCategoryResponse item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
        {
            menuOutputViewModels.Add(new MenuOutputViewModel()
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
            });
        }
        #endregion

        menuOutputViewModels = menuOutputViewModels.OrderByDescending(m => m.Id).ThenByDescending(m => m.CategoryId)
            .ToList();

        MemoryStream stream = new MemoryStream();

        using (SpreadsheetDocument spreadsheetDocument =
               SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
            Sheet sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

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
                CreateCell(_localizer[nameof(MenuOutputViewModel.Id).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.CategoryId).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.Name).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.DisplayName).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.IconPath).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.Controller).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.Action).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.Role).ToString()].Value),
                CreateCell(_localizer[nameof(MenuOutputViewModel.Order).ToString()].Value)
            );
            sheetData.Append(headerRow);

            foreach (MenuOutputViewModel item in menuOutputViewModels)
            {
                Row dataRow = new Row();
                dataRow.Append(
                    CreateCell(item.Id.ToString()),
                    CreateCell(item.CategoryId?.ToString() ?? string.Empty),
                    CreateCell(item.Name),
                    CreateCell(item.DisplayName),
                    CreateCell(item.IconPath),
                    CreateCell(item.Controller),
                    CreateCell(item.Action),
                    CreateCell(item.Role),
                    CreateCell(item.Order.ToString())
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

    #region PrivateNote

    #region Create

    #region Write

    #region PrivateNoteBoard

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WritePrivateNoteBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(boardInputViewModel.Locked));

            if (ModelState.IsValid)
            {
                boardInputViewModel.Content ??= ""; // If no content was entered

                #region HtmlSanitizer

                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;

                #endregion

                #region Decrypt img tag file path

                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(boardInputViewModel.Content);

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;

                #endregion

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new
                    {
                        result = false,
                        error = _localizer["Title length is must be between 1 and 100 characters."].Value
                    });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new
                    {
                        result = false,
                        error = _localizer["content length is must be between 0 and 16384 characters."].Value
                    });
                }

                if (boardInputViewModel.UploadedFile != null) // If a file was attached
                {
                    if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new
                            { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new
                        {
                            result = false,
                            error = _localizer["uploaded file size must be smaller than 10MB."].Value
                        });
                    }
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        AccountResponse loginedAccount =
                            JsonConvert.DeserializeObject<AccountResponse>(
                                Encoding.Default.GetString(resultByte)); // Get Session
                        BoardResponse board = new();

                        if (loginedAccount.Role == Role.Admin) // If administrator
                        {
                            board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // Private note
                            board.Title = boardInputViewModel.Title;
                            board.Content = boardInputViewModel.Content;
                            board.Writer = loginedAccount.Nickname;
                            board.Created = DateTime.UtcNow;
                            board.Updated = DateTime.UtcNow;
                            board.View = 0;
                            board.Deleted = false;
                            //board.Locked = boardInputViewModel.Locked;
                            board.Noticed = boardInputViewModel.Noticed;
                        }
                        else if (loginedAccount.Role == Role.User) // If regular user
                        {
                            board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // Private note
                            board.Title = boardInputViewModel.Title;
                            board.Content = boardInputViewModel.Content;
                            board.Writer = loginedAccount.Nickname;
                            board.Created = DateTime.UtcNow;
                            board.Updated = DateTime.UtcNow;
                            board.View = 0;
                            board.Deleted = false;
                            //board.Locked = boardInputViewModel.Locked;
                        }
                        else // If non-logged in user
                        {
                            await transaction.RollbackAsync();
                            return Json(new
                                { result = false, error = _localizer["Please Login to write board"].Value });
                        }

                        await _boardRepository.WriteBoardAsync(board.Adapt<BoardRequest>());

                        #region Save attached file

                        if (boardInputViewModel.UploadedFile != null) // If attached file exists
                        {
                            if (boardInputViewModel.UploadedFile
                                    .Length is
                                > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                var createdBoardId = board.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Management",
                                    EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                                    $"{createdBoardId}");

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string attachedFile =
                                    $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                                using (MemoryStream ms = new())
                                {
                                    await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                                    _ = await _fileClient.UploadAsync(ms.ToArray(), boardInputViewModel.UploadedFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl);
                                }

                                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(
                                    new BoardAttachedFileRequest()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile
                                            .FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path =
                                            $"upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}"
                                    });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new
                                {
                                    result = false,
                                    errorMessage = _localizer["File Size must be smaller than 10MB."].Value
                                });
                            }
                        }

                        #endregion
                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true, message = _localizer["The board has been successfully created."].Value
                        });
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new
                            { result = false, error = _localizer["Please Login to write board"].Value });
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
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region PrivateNoteComment

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> WritePrivateNoteComment(
        [FromBody] BoardCommentInputViewModel boardCommentInputViewModel)
    {
        try
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(boardCommentInputViewModel.Content))
                {
                    return Json(new { result = false, error = _localizer["Please enter a comment."].Value });
                }

                #region HtmlSanitizer

                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardCommentInputViewModel.Content);
                boardCommentInputViewModel.Content = sanitized;

                #endregion

                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        AccountResponse loginedAccount =
                            JsonConvert.DeserializeObject<AccountResponse>(
                                Encoding.Default.GetString(resultByte)); // Get Session

                        IEnumerable<BoardResponse> privateNoteBoards =
                            await _boardRepository.GetOneTypeBoardsAsync(
                                EnumHelper.GetDescription(BoardType.PrivateNote));
                        privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                            .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                        BoardResponse privateNoteBoard =
                            privateNoteBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                        if (privateNoteBoard.Id != boardCommentInputViewModel.BoardId)
                        {
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }

                        IEnumerable<BoardCommentResponse> totalBoardCommentsInSelectedBoard =
                            await _boardCommentRepository.GetOneBoardCommentsAsync(boardCommentInputViewModel
                                .BoardId);

                        BoardCommentResponse boardComment = new()
                        {
                            BoardId = boardCommentInputViewModel.BoardId,
                            Order = totalBoardCommentsInSelectedBoard?.Count() ?? default,
                            AvatarImagePath = loginedAccount.AvatarImagePath,
                            Writer = loginedAccount.Nickname,
                            Content = boardCommentInputViewModel.Content,
                            Created = DateTime.UtcNow,
                            Deleted = false
                        };
                        await _boardCommentRepository.WriteBoardCommentAsync(boardComment.Adapt<BoardCommentRequest>());

                        return Json(new
                        {
                            result = true, boardId = boardCommentInputViewModel.BoardId,
                            page = boardCommentInputViewModel.DetailCurrentPage
                        });
                    }
                    else
                    {
                        return Json(new
                            { result = false, error = _localizer["Please Login to write comment."].Value });
                    }
                }
                catch
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
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

    #endregion

    #region Summernote Image File Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> UploadImageFile(IFormFile summernoteImageFile)
    {
        if (summernoteImageFile == null)
        {
            return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
        }

        if (summernoteImageFile.Length <= 0 || summernoteImageFile.Length > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
        }

        // Extension check
        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            return Ok(new { result = false, errorMessage = _localizer["Only .jpg, .jpeg or .png file allowed."].Value });
        }

        try
        {
            // Check if it's an actual image (Magick.NET)
            using var image = new MagickImage(summernoteImageFile.OpenReadStream());
            // If Magick.NET throws an exception, it's not an image or an unsupported format
        }
        catch (MagickException)
        {
            return Ok(new { result = false, errorMessage = _localizer["Invalid image file."].Value });
        }

        _ = HttpContext.Session.TryGetValue(
            Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account),
            out byte[] resultByte);

        var loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));

        string summernoteImagePath = Path.Combine("upload", "Management",
            EnumHelper.GetDescription(BoardType.PrivateNote), "summernote", "images");
        string imageFile = $"{Guid.NewGuid():N}{ext}";
        string filePath = Path.Combine(summernoteImagePath, imageFile);

        bool uploadResult;
        using (MemoryStream ms = new())
        {
            await summernoteImageFile.CopyToAsync(ms);
            uploadResult = await _fileClient.UploadAsync(ms.ToArray(), summernoteImageFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl);
        }

        if (uploadResult)
        {
            var fileStream = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);
            return Ok(new
            {
                result = true,
                file = File(fileStream, summernoteImageFile.ContentType, imageFile),
                filePath = _rsa.Encrypt(filePath)
            });
        }
        else
        {
            return Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value });
        }
    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write

    public async Task<IActionResult> PrivateNote(string method = "list", int? boardId = null, int page = 1,
        string searchType = "", string searchText = "")
    {
        if (method == "write") // Write
        {
            PrivateNoteOutputViewModel privateNoteOutputViewModel = new();

            #region Write board

            privateNoteOutputViewModel.Method = method;

            #endregion

            bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out _); // Check if account session exists after login

            return isAccountSessionExist ? View(privateNoteOutputViewModel) : RedirectToAction("Login", "Account");
        }
        else if (method == "detail") // Detail view
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte); // If writer or administrator
            AccountResponse loginedAccount =
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)); // Get Session

            if (boardId == null)
            {
                return RedirectToAction("PrivateNote", "Management");
            }

            PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
            {
                Method = method,
                DetailCurrentPage = page,
                DetailBoardId = boardId ?? default
            };

            IEnumerable<BoardResponse> privateNoteBoards =
                await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFileResponse> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                BoardResponse privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                if (privateNoteBoard.Locked)
                {
                    if (!(loginedAccount.Nickname == privateNoteBoard.Writer || loginedAccount.Role == Role.Admin))
                    {
                        return RedirectToAction("PrivateNote", "Management");
                    }
                }

                privateNoteBoard.View++;
                await _boardRepository.UpdateBoardAsync(privateNoteBoard.Adapt<BoardRequest>());
                BoardAttachedFileResponse attachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                #region Encrypt img tag file path

                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(privateNoteBoard.Content);
                bool isImgTagIncluded = false;

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    string filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    byte[] fileData = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        string base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }
                }

                privateNoteBoard.Content = htmlDocument.DocumentNode.OuterHtml;

                #endregion

                privateNoteOutputViewModel.BoardOutputViewModel =
                    new Maroik.WebSite.Models.ViewModels.Management.BoardOutputViewModel()
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
                        IsImgTagIncluded = isImgTagIncluded
                    };

                if (!string.IsNullOrEmpty(privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ??
                                          ""))
                {
                    byte[] fileData = await _fileClient.DownloadAsync(
                        privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(
                                privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "",
                                out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = contentType;
                    }

                // Preload comments and accounts for detail view
                privateNoteOutputViewModel.DetailBoardComments = (await _boardCommentRepository.GetOneBoardCommentsAsync(boardId ?? 0))?.ToList() ?? [];
                privateNoteOutputViewModel.AllAccounts = await _accountRepository.GetAllAsync() ?? [];
                privateNoteOutputViewModel.LoginedAccount = loginedAccount;
                privateNoteOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount?.TimeZoneIanaId ?? "";

                }

                return View(privateNoteOutputViewModel);
            }
            catch
            {
                return RedirectToAction("PrivateNote", "Management");
            }
        }
        else if (method == "edit") // Edit
        {
            if (boardId == null)
            {
                return RedirectToAction("PrivateNote", "Management");
            }

            PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
            {
                Method = method,
                EditCurrentPage = page,
                EditBoardId = boardId ?? default
            };

            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            IEnumerable<BoardResponse> privateNoteBoards =
                await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
            List<BoardAttachedFileResponse> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

            try
            {
                BoardResponse privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                if (privateNoteBoard.Writer != loginedAccount.Nickname) // If writer and logged-in user are not the same
                {
                    return RedirectToAction("PrivateNote", "Management");
                }

                BoardAttachedFileResponse attachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                #region Encrypt img tag file path

                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(privateNoteBoard.Content);
                bool isImgTagIncluded = false;

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    isImgTagIncluded = true;
                    string filePath = imgTag.GetAttributeValue("alt", "");

                    // Method to download file
                    byte[] fileData = await _fileClient.DownloadAsync(filePath, ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(filePath, out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        string base64Data = Convert.ToBase64String(fileData);

                        // Store base64 data in img tag (use data-file attribute instead of src)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }
                }

                privateNoteBoard.Content = htmlDocument.DocumentNode.OuterHtml;

                #endregion

                privateNoteOutputViewModel.BoardOutputViewModel =
                    new Maroik.WebSite.Models.ViewModels.Management.BoardOutputViewModel()
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

                if (!string.IsNullOrEmpty(privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ??
                                          ""))
                {
                    byte[] fileData = await _fileClient.DownloadAsync(
                        privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "", ServerSetting.FileStorageBaseUrl);

                    if (fileData != null)
                    {
                        // Use FileExtensionContentTypeProvider to extract ContentType
                        FileExtensionContentTypeProvider provider = new();

                        // Set default contentType to "application/octet-stream"
                        if (!provider.TryGetContentType(
                                privateNoteOutputViewModel?.BoardOutputViewModel?.BoardAttachedFilePath ?? "",
                                out string contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // Convert file data to Base64
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType = contentType;
                    }
                }

                return View(privateNoteOutputViewModel);
            }
            catch
            {
                return RedirectToAction("PrivateNote", "Management");
            }
        }
        else // List
        {
            bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            AccountResponse loginedAccount =
                JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)); // Get Session

            #region Board default paging logic

            const int pageSize = 5;
            if (page < 1)
            {
                page = 1;
            }

            #endregion

            #region Logic to get all related board data

            IEnumerable<BoardResponse> privateNoteBoards =
                await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);

            #endregion

            #region Search keyword logic

            if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
            {
                if (isAccountSessionExist) // If logged in
                {
                    if (loginedAccount.Role == Role.Admin) // Administrator can see all posts
                    {
                        privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText));
                    }
                    else if (loginedAccount.Role == Role.User) // User can see regular posts + their own private posts
                    {
                        List<BoardResponse> searchPrivateNoteBoards = [];
                        List<BoardResponse> noLockedPrivateNoteBoards = privateNoteBoards
                            .Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                        List<BoardResponse> myLockedPrivateNoteBoards = privateNoteBoards
                            .Where(a => a.Title.Contains(searchText))
                            .Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                        searchPrivateNoteBoards.AddRange(noLockedPrivateNoteBoards);
                        searchPrivateNoteBoards.AddRange(myLockedPrivateNoteBoards);

                        privateNoteBoards = searchPrivateNoteBoards;
                    }
                    else // If logged in but has neither Admin nor User role, it's a bug
                    {
                    }
                }
                else // If not logged in
                {
                    privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText))
                        .Where(a => !a.Locked); // When not logged in, private posts should not be searchable
                }
            }
            else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
            {
                privateNoteBoards = privateNoteBoards.Where(a => a.Writer.Contains(searchText));
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

            privateNoteOutputViewModel.Pager = new Pager(privateNoteBoards.Count(), page, pageSize);

            #endregion

            #region Board data logic

            privateNoteOutputViewModel.Boards = privateNoteBoards.Skip((page - 1) * pageSize)
                .Take(privateNoteOutputViewModel.Pager.PageSize).ToList();

            #endregion

            #region Preload comment counts and attached files for view
            privateNoteOutputViewModel.LoginedAccount = loginedAccount;
            privateNoteOutputViewModel.LoginedAccountTimeZoneIanaId = loginedAccount?.TimeZoneIanaId ?? "";
            privateNoteOutputViewModel.AllAccounts = await _accountRepository.GetAllAsync() ?? [];
            List<BoardAttachedFileResponse> allPrivateAttachedFiles = await _boardAttachedFileRepository.GetAllAsync() ?? [];
            foreach (var board in privateNoteOutputViewModel.Boards)
            {
                privateNoteOutputViewModel.CommentCountByBoardId[board.Id] =
                    (await _boardCommentRepository.GetOneBoardCommentsAsync(board.Id))?.Count(c => !c.Deleted) ?? 0;
                privateNoteOutputViewModel.AttachedFilesByBoardId[board.Id] =
                    allPrivateAttachedFiles.Where(f => f.BoardId == board.Id && !string.IsNullOrEmpty(f.Name)).ToList();
            }
            #endregion

            ViewBag.SelectedSearchType = searchType;
            ViewBag.TypedSearchText = searchText;
            return View(privateNoteOutputViewModel);
        }
    }

    #endregion

    #region IsBoardExists

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> IsBoardExists(int id)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            IEnumerable<BoardResponse> privateNoteBoards =
                await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            if (privateNoteBoards == null)
            {
                return Json(new { result = false, error = _localizer["No PrivateNoteBoard exists"].Value });
            }
            else
            {
                BoardResponse tempPrivateNoteBoard = privateNoteBoards.Where(a => a.Id == id).FirstOrDefault();

                return tempPrivateNoteBoard == null
                    ? Json(new { result = false, error = _localizer["Input is invalid"].Value })
                    : (IActionResult)Json(new { result = true, PrivateNoteBoard = tempPrivateNoteBoard });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #region Update

    #region Edit

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> EditPrivateNoteBoard(BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(boardInputViewModel.Locked));

            if (ModelState.IsValid)
            {
                boardInputViewModel.Content ??= ""; // If no content was entered

                #region HtmlSanitizer

                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;

                #endregion

                #region Decrypt img tag file path

                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(boardInputViewModel.Content);

                foreach (HtmlNode imgTag in htmlDocument.DocumentNode.Descendants("img") ?? [])
                {
                    string encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;

                #endregion

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // Title length
                {
                    return Json(new
                    {
                        result = false,
                        error = _localizer["Title length is must be between 1 and 100 characters."].Value
                    });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new
                    {
                        result = false,
                        error = _localizer["content length is must be between 0 and 16384 characters."].Value
                    });
                }

                if (boardInputViewModel.UploadedFile != null) // If a file was attached
                {
                    if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // Only zip extension allowed
                    {
                        return Json(new
                            { result = false, error = _localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // Only 10MB or less allowed
                    {
                        return Json(new
                        {
                            result = false,
                            error = _localizer["uploaded file size must be smaller than 10MB."].Value
                        });
                    }
                }
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    bool isAccountSessionExist = HttpContext.Session.TryGetValue(
                        Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                            .Utilities.Session.Account), out byte[] resultByte);

                    if (isAccountSessionExist)
                    {
                        AccountResponse loginedAccount =
                            JsonConvert.DeserializeObject<AccountResponse>(
                                Encoding.Default.GetString(resultByte)); // Get Session

                        IEnumerable<BoardResponse> privateNoteBoards =
                            await _boardRepository.GetOneTypeBoardsAsync(
                                EnumHelper.GetDescription(BoardType.PrivateNote));
                        privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                            .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                        BoardResponse tempPrivateNoteBoard = privateNoteBoards.Where(a => a.Id == boardInputViewModel.Id)
                            .FirstOrDefault();

                        if (tempPrivateNoteBoard.Writer != loginedAccount.Nickname)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                        }

                        tempPrivateNoteBoard.Title = boardInputViewModel.Title;
                        tempPrivateNoteBoard.Content = boardInputViewModel.Content;
                        //tempPrivateNoteBoard.Locked = boardInputViewModel.Locked;
                        tempPrivateNoteBoard.Updated = DateTime.UtcNow;

                        await _boardRepository.UpdateBoardAsync(tempPrivateNoteBoard.Adapt<BoardRequest>());

                        List<BoardAttachedFileResponse> boardAttachedFiles =
                            await _boardAttachedFileRepository?.GetAllAsync();
                        BoardAttachedFileResponse previousBoardAttachedFile =
                            boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                        #region Save attached file

                        #region Existing attached file exists && New attached file exists

                        if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile
                                    .Length is
                                > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                int createdBoardId = boardInputViewModel.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Management",
                                    EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                                    $"{createdBoardId}");
                                if (!Directory.Exists(boardAttachedFilePath))
                                {
                                    _ = Directory.CreateDirectory(boardAttachedFilePath);
                                }

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string attachedFile =
                                    $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                                using (MemoryStream ms = new())
                                {
                                    await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                                    _ = await _fileClient.UploadAsync(ms.ToArray(), boardInputViewModel.UploadedFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl);
                                }

                                previousBoardAttachedFile.Size =
                                    Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                previousBoardAttachedFile.Name =
                                    Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Extension =
                                    Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                previousBoardAttachedFile.Path =
                                    $"upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}";

                                await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(
                                    previousBoardAttachedFile.Adapt<BoardAttachedFileRequest>());
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The board has been successfully updated."].Value
                                });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new
                                {
                                    result = false,
                                    errorMessage = _localizer["File Size must be smaller than 10MB."].Value
                                });
                            }
                        }

                        #endregion

                        #region Existing attached file exists && New attached file does not exist

                        else if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                        {
                            previousBoardAttachedFile.Size = 0;
                            previousBoardAttachedFile.Name = "";
                            previousBoardAttachedFile.Extension = "";
                            previousBoardAttachedFile.Path = "";

                            await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(
                                previousBoardAttachedFile.Adapt<BoardAttachedFileRequest>());
                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The board has been successfully updated."].Value
                            });
                        }

                        #endregion

                        #region Existing attached file does not exist && New attached file exists

                        else if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                        {
                            if (boardInputViewModel.UploadedFile
                                    .Length is
                                > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : file size 0MB or more, 10MB or less
                            {
                                int createdBoardId = boardInputViewModel.Id; // Add logic for attached file FK

                                string boardAttachedFilePath = Path.Combine("upload", "Management",
                                    EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                                    $"{createdBoardId}");

                                string guid = Guid.NewGuid().ToString().ToUpper();
                                string attachedFile =
                                    $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                                using (MemoryStream ms = new())
                                {
                                    await boardInputViewModel.UploadedFile.CopyToAsync(ms);
                                    _ = await _fileClient.UploadAsync(ms.ToArray(), boardInputViewModel.UploadedFile.ContentType, filePath, ServerSetting.FileStorageBaseUrl);
                                }

                                await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(
                                    new BoardAttachedFileRequest()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile
                                            .FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path =
                                            $"upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}"
                                    });
                                await transaction.CommitAsync();
                                return Json(new
                                {
                                    result = true,
                                    message = _localizer["The board has been successfully updated."].Value
                                });
                            }
                            else
                            {
                                await transaction.RollbackAsync();
                                return Ok(new
                                {
                                    result = false,
                                    errorMessage = _localizer["File Size must be smaller than 10MB."].Value
                                });
                            }
                        }

                        #endregion

                        #region Existing attached file does not exist && New attached file does not exist

                        else
                        {
                            await transaction.CommitAsync();
                            return Json(new
                            {
                                result = true,
                                message = _localizer["The board has been successfully updated."].Value
                            });
                        }

                        #endregion

                        #endregion
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });
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
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #region Delete

    #region Board

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteBoard([FromBody] BoardInputViewModel boardInputViewModel)
    {
        try
        {
            _ = HttpContext.Session.TryGetValue(
                Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                    .Utilities.Session.Account), out byte[] resultByte);
            AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

            IEnumerable<BoardResponse> privateNoteBoards =
                await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            if (privateNoteBoards == null)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                BoardResponse tempPrivateNoteBoard =
                    privateNoteBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                if (tempPrivateNoteBoard == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    if (loginedAccount.Nickname == tempPrivateNoteBoard.Writer) // If visitor is writer
                    {
                        #region Delete board

                        tempPrivateNoteBoard.Deleted = true;
                        await _boardRepository.DeleteBoardAsync(tempPrivateNoteBoard.Adapt<BoardRequest>());

                        #endregion

                        return Json(new
                        {
                            result = true, message = _localizer["The board has been successfully deleted."].Value
                        });
                    }
                    else
                    {
                        return Json(new
                            { result = true, message = _localizer["You do not have permission to delete."].Value });
                    }
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region BoardComment

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    [RequiredHttpPostAccess(Role = Role.User)]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            BoardCommentResponse boardComment = await _boardCommentRepository.GetBoardCommentAsync(id);

            if (boardComment == null)
            {
                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
            }
            else
            {
                _ = HttpContext.Session.TryGetValue(
                    Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite
                        .Utilities.Session.Account), out byte[] resultByte);
                AccountResponse loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert
                    .DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte)).Email);

                if (loginedAccount.Nickname == boardComment.Writer) // If deleter is writer
                {
                    #region Delete post

                    boardComment.Deleted = true;
                    await _boardCommentRepository.DeleteBoardCommentAsync(boardComment.Adapt<BoardCommentRequest>());

                    #endregion

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new
                        { result = true, message = _localizer["You do not have permission to delete."].Value });
                }
            }
        }
        catch
        {
            return Json(new { result = false, error = _localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion
}