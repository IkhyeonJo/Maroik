using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Ganss.Xss;
using HtmlAgilityPack;
using ImageMagick;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Contracts;
using Maroik.WebSite.Models;
using Maroik.WebSite.Models.ViewModels.Management;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers;

public class ManagementController(
    IHtmlLocalizer<ManagementController> localizer,
    IBoardRepository boardRepository,
    IBoardAttachedFileRepository boardAttachedFileRepository,
    IBoardCommentRepository boardCommentRepository,
    IAccountRepository accountRepository,
    IHostEnvironment hostEnvironment,
    ICategoryRepository categoryRepository,
    ISubCategoryRepository subCategoryRepository,
    IFileRepository fileRepository,
    IClamavRepository clamavRepository,
    ApplicationDbContext context)
    : Controller
{
    private readonly RSA _rsa = new(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey);

    #region Profile

    #region Read

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;
        var tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

        return View(new ProfileOutputViewModel
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
            return Ok(new { result = false, errorMessage = localizer["Please attach a file"].Value });
        }

        var file = profileInputViewModel.ProfileAvatarFiles[0];

        if (file.Length is <= 0 or > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Ok(new { result = false, errorMessage = localizer["Only .jpg or jpeg or .png file allowed"].Value });
        }

        // ClamAV 검사 전 메모리 스트림 복사
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        if (!await clamavRepository.ScanWithClamavAsync(memoryStream))
        {
            return BadRequest("File may be infected with a virus.");
        }

        // 이미지 유효성 확인 (Magick.NET 사용, SVG 차단 포함)
        memoryStream.Position = 0;
        try
        {
            using var image = new MagickImage(memoryStream);
            if (image.Format == MagickFormat.Svg)
            {
                return Ok(new { result = false, errorMessage = localizer["SVG format is not allowed"].Value });
            }
        }
        catch
        {
            return Ok(new { result = false, errorMessage = localizer["Invalid image file."].Value });
        }

        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account),
            out var resultByte);

        var avatarPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", "Avatar");
        if (!Directory.Exists(avatarPath))
        {
            Directory.CreateDirectory(avatarPath);
        }

        var guid = Guid.NewGuid().ToString().ToUpper();
        var avatarFile = $"{guid}{extension}";
        var filePath = Path.Combine(avatarPath, avatarFile);

#pragma warning disable SCS0018
        await using (var stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018
        {
            memoryStream.Position = 0;
            await memoryStream.CopyToAsync(stream);
            await stream.FlushAsync();
        }

        // 실행 권한 제거 (chmod 644)
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
            var tempAccount = await accountRepository.GetAccountByEmailAsync(
                JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            tempAccount.AvatarImagePath = $"/upload/Management/Profile/Avatar/{avatarFile}";
            await accountRepository.UpdateAccountAsync(tempAccount);

            return Ok(new { result = true });
        }
        catch
        {
            return Ok(new { result = false, errorMessage = localizer["Input is invalid"].Value });
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

        if (!ModelState.IsValid) return RedirectToAction("Profile", "Management");
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);
            tempAccount.TimeZoneIanaId = profileInputViewModel.TimeZoneIanaId;
            await accountRepository.UpdateAccountAsync(tempAccount);

            HttpContext.Session.Remove(
                EnumHelper.GetDescription(Session.Account));
            tempAccount.HashedPassword = null;
            HttpContext.Session.Set(
                EnumHelper.GetDescription(Session.Account),
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));
        }
        catch
        {
            Console.WriteLine(localizer["Input is invalid"].Value);
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

        if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
        try
        {
            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (!_rsa.Verify(profileInputViewModel.Password ?? "", tempAccount.HashedPassword)) // right password
                return Json(new
                    { result = false, error = localizer["Invalid password. Please check again."].Value });
            tempAccount.HashedPassword = _rsa.Sign(profileInputViewModel.NewPassword);
            await accountRepository.UpdateAccountAsync(tempAccount);
            return Json(new
                { result = true, message = localizer["Password change successfully done."].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });

            #region Email 형식 체크

            static bool IsValidEmail(string email)
            {
                if (!MailAddress.TryCreate(email, out var mailAddress))
                {
                    return false;
                }

                // And if you want to be more strict:
                var hostParts = mailAddress.Host.Split('.');
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

                return !mailAddress.User.Contains(' ') && mailAddress.User.Split('.').All(p => p != string.Empty);
            }

            if (!IsValidEmail(accountInputViewModel.Email)) // 유효하지 않은 메일 형식
            {
                return Json(new { result = false, error = localizer["Invalid email form"].Value });
            }

            #endregion

            #region Password 형식 체크

            if (!new Regex(
                        "^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$")
                    .IsMatch(accountInputViewModel.Password)) // 유효하지 않은 비밀번호 형식
            {
                return Json(new
                {
                    result = false,
                    error = localizer[
                            "Password must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)"]
                        .Value
                });
            }

            #endregion

            #region Nickname 형식 체크

            if (string.IsNullOrEmpty(accountInputViewModel.Nickname))
            {
                return Json(new { result = false, error = localizer["Please enter Nickname"].Value });
            }

            if (accountInputViewModel.Nickname.Length is not (>= 1 and <= 255))
            {
                return Json(new
                {
                    result = false, error = localizer["Nickname is must be between 1 and 255 characters"].Value
                });
            }

            #endregion

            #region Role 체크

            if (accountInputViewModel.Role is not (Role.Admin or Role.User))
            {
                accountInputViewModel.Role = Role.User;
            }

            #endregion

            #region TimeZone 형식 체크

            if (string.IsNullOrEmpty(accountInputViewModel.TimeZoneIanaId))
            {
                return Json(new { result = false, error = localizer["Please select time zone"].Value });
            }

            #endregion

            #region 이미 입력된 이메일로 계정이 있는 지 확인

            if (await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email) != null)
            {
                return Json(new
                    { result = false, error = localizer["This account has already been created."].Value });
            }

            #endregion

            #region 계정 생성

            await accountRepository.CreateAccountAsync
            (
                new Account
                {
                    Email = accountInputViewModel.Email,
                    HashedPassword = _rsa.Sign(accountInputViewModel.Password), // 암호화
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
            return Json(new { result = true, message = localizer["Account create successfully done."].Value });

            #endregion

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> Account(string wholeSearch)
    {
        if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View(); // ajax
        List<AccountOutputViewModel> accountOutputViewModels = [];
        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);

        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        #region Accounts

        accountOutputViewModels.AddRange(from item in await accountRepository.GetAllAsync()
            where string.IsNullOrEmpty(wholeSearch) || item.Email.Contains(wholeSearch) || (item.HashedPassword ?? "").Contains(wholeSearch) || (item.Nickname ?? "").Contains(wholeSearch) || (item.AvatarImagePath ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || (item.TimeZoneIanaId ?? "").Contains(wholeSearch) || item.Locked.ToString().Contains(wholeSearch) || item.LoginAttempt.ToString().Contains(wholeSearch) || item.EmailConfirmed.ToString().Contains(wholeSearch) || item.AgreedServiceTerms.ToString().Contains(wholeSearch) || (item.RegistrationToken ?? "").Contains(wholeSearch) || (item.ResetPasswordToken ?? "").Contains(wholeSearch) || item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture).Contains(wholeSearch) || item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString(CultureInfo.InvariantCulture).Contains(wholeSearch) || (item.Message ?? "").Contains(wholeSearch) || item.Deleted.ToString().Contains(wholeSearch)
            select new AccountOutputViewModel
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

        #endregion

        var result = accountOutputViewModels.OrderBy(m => m.Email)
            .ThenBy(m => m.Nickname).ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated)
            .AsQueryable();
        return PartialView("_AccountGrid", result);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> IsAccountExists(string email)
    {
        try
        {
            var tempAccount = await accountRepository.GetAccountByEmailAsync(email);

            return tempAccount == null
                ? Json(new
                {
                    result = false, error = localizer["Fail to find the account by given email address"].Value
                })
                : (IActionResult)Json(new { result = true, account = tempAccount });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            var tempAccount = await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

            if (tempAccount == null)
            {
                return Json(new { result = false, error = localizer["Email address is wrong"].Value });
            }

            tempAccount.Email = accountInputViewModel.Email;
            tempAccount.HashedPassword = _rsa.Sign(accountInputViewModel.Password); // 암호화
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

            await accountRepository.UpdateAccountAsync(tempAccount);

            return Json(new { result = true, message = localizer["Successfully updated the account"].Value });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteAccount([FromBody] AccountInputViewModel accountInputViewModel)
    {
        try
        {
            var tempAccount = await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

            if (tempAccount == null)
            {
                return Json(new
                {
                    result = false, error = localizer["Fail to find the account by given email address"].Value
                });
            }

            tempAccount.Deleted = true;
            await accountRepository.UpdateAccountAsync(tempAccount);
            return Json(new { result = true, message = localizer["Successfully deleted the account"].Value });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelAccount(string fileName = "")
    {
        await Task.Yield();

        List<AccountOutputViewModel> accountOutputViewModels = [];
        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        #region Accounts

        accountOutputViewModels.AddRange((await accountRepository.GetAllAsync()).Select(item => new AccountOutputViewModel
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
        }));

        accountOutputViewModels = accountOutputViewModels.OrderBy(m => m.Email).ThenBy(m => m.Nickname)
            .ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();

        #endregion

        MemoryStream stream = new();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            var sheets = document.WorkbookPart?.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet()
            {
                Id = document.WorkbookPart?.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets?.Append(sheet);

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            Cell CreateCell(string value)
            {
                return new Cell
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(AccountOutputViewModel.Email)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.HashedPassword)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Nickname)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.AvatarImagePath)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Role)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.TimeZoneIanaId)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Locked)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.LoginAttempt)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.EmailConfirmed)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.AgreedServiceTerms)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.RegistrationToken)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.ResetPasswordToken)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Created)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Updated)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Message)].Value),
                CreateCell(localizer[nameof(AccountOutputViewModel.Deleted)].Value)
            );
            sheetData?.Append(headerRow);

            var recordIndex = 2;
            foreach (var item in accountOutputViewModels)
            {
                var recordRow = new Row()
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
                    CreateCell(item.Created.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Updated.ToString(CultureInfo.InvariantCulture)),
                    CreateCell(item.Message),
                    CreateCell(item.Deleted.ToString())
                );
                sheetData?.Append(recordRow);

                recordIndex++;
            }

            workbookPart.Workbook.Save();
        }

        stream.Position = 0;
        var excelName =
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            Category category = new()
            {
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Controller = menuInputViewModel.Controller,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            };
            await categoryRepository.CreateCategoryAsync(category);

            return Json(new { result = true, message = localizer["Successfully created the category"].Value });

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            SubCategory subCategory = new()
            {
                CategoryId = menuInputViewModel.CategoryId,
                Name = menuInputViewModel.Name,
                DisplayName = menuInputViewModel.DisplayName,
                IconPath = menuInputViewModel.IconPath,
                Action = menuInputViewModel.Action,
                Role = menuInputViewModel.Role,
                Order = menuInputViewModel.Order
            };
            await subCategoryRepository.CreateSubCategoryAsync(subCategory);

            return Json(new
                { result = true, message = localizer["Successfully created the subCategory"].Value });

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #region Read

    [HttpGet]
    public async Task<IActionResult> Menu(string wholeSearch)
    {
        if (HttpContext.Request.Headers.XRequestedWith != "XMLHttpRequest") return View();
        List<MenuOutputViewModel> menuOutputViewModels = [];
            
        #region AdminCategories
        menuOutputViewModels.AddRange(from item in await categoryRepository.GetCategoryByRoleAsync(Role.Admin)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Controller ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });
        #endregion

        #region UserCategories
        menuOutputViewModels.AddRange(from item in await categoryRepository.GetCategoryByRoleAsync(Role.User)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Controller ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });
        #endregion

        #region AnonymousCategories

        menuOutputViewModels.AddRange(from item in await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Controller ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });

        #endregion

        #region AdminSubCategories

        menuOutputViewModels.AddRange(from item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || item.CategoryId.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });

        #endregion

        #region UserSubCategories

        menuOutputViewModels.AddRange(from item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || item.CategoryId.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });

        #endregion

        #region AnonymousSubCategories

        menuOutputViewModels.AddRange(from item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous)
            where string.IsNullOrEmpty(wholeSearch) || item.Id.ToString().Contains(wholeSearch) || item.CategoryId.ToString().Contains(wholeSearch) || (item.Name ?? "").Contains(wholeSearch) || (item.DisplayName ?? "").Contains(wholeSearch) || (item.IconPath ?? "").Contains(wholeSearch) || (item.Action ?? "").Contains(wholeSearch) || (item.Role ?? "").Contains(wholeSearch) || item.Order.ToString().Contains(wholeSearch)
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
            });

        #endregion

        var result = menuOutputViewModels.OrderByDescending(m => m.Id)
            .ThenByDescending(m => m.CategoryId).AsQueryable();
        return PartialView("_MenuGrid", result);
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
            menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.Admin)).Select(item => new MenuOutputViewModel
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
            }));
            #endregion

            #region UserCategories
            menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.User)).Select(item => new MenuOutputViewModel
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
            }));
            #endregion

            #region AnonymousCategories
            menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous)).Select(item => new MenuOutputViewModel
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
            }));
            #endregion

            var selectedCategory = menuOutputViewModels.FirstOrDefault(a => a.Id == id);
            if (selectedCategory == null)
            {
                throw new Exception(); // DB에서 못 찾음
            }

            return Json(new
                { result = true, category = menuOutputViewModels.FirstOrDefault(a => a.Id == id) });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin)).Select(item => new MenuOutputViewModel
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
            }));
            #endregion

            #region UserSubCategories
            menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User)).Select(item => new MenuOutputViewModel
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
            }));
            #endregion

            #region AnonymousSubCategories

            menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous)).Select(item => new MenuOutputViewModel
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
            }));

            #endregion

            var selectedCategory = menuOutputViewModels.FirstOrDefault(a => a.Id == id);
            if (selectedCategory == null)
            {
                throw new Exception(); // DB에서 못 찾음
            }

            return Json(new
                { result = true, subCategory = menuOutputViewModels.FirstOrDefault(a => a.Id == id) });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            Category category = new()
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
            await categoryRepository.UpdateCategoryAsync(category);

            return Json(new { result = true, message = localizer["Successfully updated the category"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            SubCategory subCategory = new()
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
            await subCategoryRepository.UpdateSubCategoryAsync(subCategory);

            return Json(new
                { result = true, message = localizer["Successfully updated the subCategory"].Value });
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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> DeleteCategory([FromBody] MenuInputViewModel menuInputViewModel)
    {
        try
        {
            _ = ModelState.Remove(nameof(menuInputViewModel
                .Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            Category category = new()
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
            await categoryRepository.DeleteCategoryAsync(category);

            return Json(new { result = true, message = localizer["Successfully deleted the category"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid)
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            SubCategory subCategory = new()
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
            await subCategoryRepository.DeleteSubCategoryAsync(subCategory);

            return Json(new
                { result = true, message = localizer["Successfully deleted the subCategory"].Value });

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
    [RequiredHttpPostAccess(Role = Role.Admin)]
    public async Task<IActionResult> ExportExcelMenu(string fileName = "")
    {
        await Task.Yield();

        List<MenuOutputViewModel> menuOutputViewModels = [];
        _ = HttpContext.Session.TryGetValue(
            EnumHelper.GetDescription(Session.Account), out var resultByte);
        var loginedAccountTimeZoneIanaId = JsonConvert
            .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).TimeZoneIanaId;

        #region AdminCategories

        menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.Admin)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        #region UserCategories

        menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.User)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        #region AnonymousCategories

        menuOutputViewModels.AddRange((await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        #region AdminSubCategories

        menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        #region UserSubCategories

        menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        #region AnonymousSubCategories

        menuOutputViewModels.AddRange((await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous)).Select(item => new MenuOutputViewModel
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
        }));

        #endregion

        menuOutputViewModels = menuOutputViewModels.OrderByDescending(m => m.Id).ThenByDescending(m => m.CategoryId)
            .ToList();

        var stream = new MemoryStream();

        using (var spreadsheetDocument =
               SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            var sheets = spreadsheetDocument.WorkbookPart?.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart?.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets?.Append(sheet);

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();

            Cell CreateCell(string value)
            {
                return new Cell()
                {
                    CellValue = new CellValue(value),
                    DataType = CellValues.String
                };
            }

            var headerRow = new Row();
            headerRow.Append(
                CreateCell(localizer[nameof(MenuOutputViewModel.Id)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.CategoryId)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.Name)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.DisplayName)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.IconPath)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.Controller)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.Action)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.Role)].Value),
                CreateCell(localizer[nameof(MenuOutputViewModel.Order)].Value)
            );
            sheetData?.Append(headerRow);

            foreach (var item in menuOutputViewModels)
            {
                var dataRow = new Row();
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
                sheetData?.Append(dataRow);
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
                boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

                #region HtmlSanitizer

                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                boardInputViewModel.Content = sanitized;

                #endregion

                #region Decrypt img tag file path

                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(boardInputViewModel.Content);

                foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                {
                    var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                    _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
                }

                boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;

                #endregion

                if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer["Title length is must be between 1 and 100 characters."].Value
                    });
                }

                if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer["content length is must be between 0 and 16384 characters."].Value
                    });
                }

                if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                {
                    if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip") // zip 확장자만 가능
                    {
                        return Json(new
                            { result = false, error = localizer["Only zip extension allowed."].Value });
                    }

                    if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                    {
                        return Json(new
                        {
                            result = false,
                            error = localizer["uploaded file size must be smaller than 10MB."].Value
                        });
                    }
                }

                await using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    var isAccountSessionExist = HttpContext.Session.TryGetValue(
                        EnumHelper.GetDescription(Session.Account), out var resultByte);

                    if (isAccountSessionExist)
                    {
                        var loginedAccount =
                            JsonConvert.DeserializeObject<Account>(
                                Encoding.Default.GetString(resultByte)); // Get Session
                        Board board = new();

                        switch (loginedAccount.Role)
                        {
                            // 관리자인 경우
                            case Role.Admin:
                                board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // 사적 메모
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.View = 0;
                                board.Deleted = false;
                                //board.Locked = boardInputViewModel.Locked;
                                board.Noticed = boardInputViewModel.Noticed;
                                break;
                            // 일반 사용자인 경우
                            case Role.User:
                                board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // 사적 메모
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.View = 0;
                                board.Deleted = false;
                                //board.Locked = boardInputViewModel.Locked;
                                break;
                            // 비로그인 사용자인 경우
                            default:
                                return Json(new
                                    { result = false, error = localizer["Please Login to write board"].Value });
                        }

                        await boardRepository.WriteBoardAsync(board);

                        #region 첨부 파일 저장

                        if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                        {
                            if (boardInputViewModel.UploadedFile
                                    .Length is
                                > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                            {
                                var createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                                var boardAttachedFilePath = Path.Combine("upload", "Management",
                                    EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                                    $"{createdBoardId}");

                                var guid = Guid.NewGuid().ToString().ToUpper();
                                var attachedFile =
                                    $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                var filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                                _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                                await boardAttachedFileRepository.SaveBoardAttachedFileAsync(
                                    new BoardAttachedFile
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
                                return Ok(new
                                {
                                    result = false,
                                    errorMessage = localizer["File Size must be smaller than 10MB."].Value
                                });
                            }
                        }

                        #endregion
                        await transaction.CommitAsync();
                        return Json(new
                        {
                            result = true, message = localizer["The board has been successfully created."].Value
                        });
                    }

                    return Json(new
                        { result = false, error = localizer["Please Login to write board"].Value });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
                    return Json(new { result = false, error = localizer["Please enter a comment."].Value });
                }

                #region HtmlSanitizer

                HtmlSanitizer sanitizer = new();
                _ = sanitizer.AllowedAttributes.Add("class");
                var sanitized = sanitizer.Sanitize(boardCommentInputViewModel.Content);
                boardCommentInputViewModel.Content = sanitized;

                #endregion

                try
                {
                    var isAccountSessionExist = HttpContext.Session.TryGetValue(
                        EnumHelper.GetDescription(Session.Account), out var resultByte);

                    if (isAccountSessionExist)
                    {
                        var loginedAccount =
                            JsonConvert.DeserializeObject<Account>(
                                Encoding.Default.GetString(resultByte)); // Get Session

                        var privateNoteBoards =
                            await boardRepository.GetOneTypeBoardsAsync(
                                EnumHelper.GetDescription(BoardType.PrivateNote));
                        privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                            .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                        var privateNoteBoard =
                            privateNoteBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                        if (privateNoteBoard != null && privateNoteBoard.Id != boardCommentInputViewModel.BoardId)
                        {
                            return Json(new { result = false, error = localizer["Input is invalid"].Value });
                        }

                        var totalBoardCommentsInSelectedBoard =
                            await boardCommentRepository.GetOneBoardCommentsAsync(boardCommentInputViewModel
                                .BoardId);

                        BoardComment boardComment = new()
                        {
                            BoardId = boardCommentInputViewModel.BoardId,
                            Order = totalBoardCommentsInSelectedBoard?.Count() ?? 0,
                            AvatarImagePath = loginedAccount.AvatarImagePath,
                            Writer = loginedAccount.Nickname,
                            Content = boardCommentInputViewModel.Content,
                            Created = DateTime.UtcNow,
                            Deleted = false
                        };
                        await boardCommentRepository.WriteBoardCommentAsync(boardComment);

                        return Json(new
                        {
                            result = true, boardId = boardCommentInputViewModel.BoardId,
                            page = boardCommentInputViewModel.DetailCurrentPage
                        });
                    }

                    return Json(new
                        { result = false, error = localizer["Please Login to write comment."].Value });
                }
                catch
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
            }

            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            return Ok(new { result = false, errorMessage = localizer["Please attach a file."].Value });
        }

        if (summernoteImageFile.Length is <= 0 or > 10 * 1024 * 1024)
        {
            return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
        }

        // 확장자 검사
        var ext = Path.GetExtension(summernoteImageFile.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
        {
            return Ok(new { result = false, errorMessage = localizer["Only .jpg, .jpeg or .png file allowed."].Value });
        }

        try
        {
            // 실제 이미지 여부 검사 (Magick.NET)
            using var image = new MagickImage(summernoteImageFile.OpenReadStream());
            // Magick.NET에서 예외가 발생하면 이미지가 아니거나 허용되지 않는 포맷임
        }
        catch (MagickException)
        {
            return Ok(new { result = false, errorMessage = localizer["Invalid image file."].Value });
        }

        var summernoteImagePath = Path.Combine("upload", "Management",
            EnumHelper.GetDescription(BoardType.PrivateNote), "summernote", "images");
        var imageFile = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(summernoteImagePath, imageFile);

        var uploadResult = await fileRepository.UploadAsync(summernoteImageFile, filePath);

        if (!uploadResult) return Ok(new { result = false, errorMessage = localizer["Input is invalid"].Value });
        var fileStream = await fileRepository.DownloadAsync(filePath);
        return Ok(new
        {
            result = true,
            file = File(fileStream, summernoteImageFile.ContentType, imageFile),
            filePath = _rsa.Encrypt(filePath)
        });

    }
    #endregion

    #endregion

    #region Read

    #region Edit, List, Detail, Write

    public async Task<IActionResult> PrivateNote(string method = "list", int? boardId = null, int page = 1,
        string searchType = "", string searchText = "")
    {
        switch (method)
        {
            // 글쓰기 (write)
            case "write":
            {
                PrivateNoteOutputViewModel privateNoteOutputViewModel = new();

                #region 게시판 글쓰기

                privateNoteOutputViewModel.Method = method;

                #endregion

                var isAccountSessionExist = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                return isAccountSessionExist ? View(privateNoteOutputViewModel) : RedirectToAction("Login", "Account");
            }
            // 상세보기 (detail)
            case "detail":
            {
                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte); // 작성자 또는 관리자인 경우
                var loginedAccount =
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])); // Get Session

                if (boardId == null)
                {
                    return RedirectToAction("PrivateNote", "Management");
                }

                PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
                {
                    Method = method,
                    DetailCurrentPage = page,
                    DetailBoardId = (int)boardId
                };

                var privateNoteBoards =
                    await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                    .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var boardAttachedFiles = await boardAttachedFileRepository.GetAllAsync();

                try
                {
                    var privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                    if (privateNoteBoard is { Locked: true })
                    {
                        if (!(loginedAccount.Nickname == privateNoteBoard.Writer || loginedAccount.Role == Role.Admin))
                        {
                            return RedirectToAction("PrivateNote", "Management");
                        }
                    }

                    if (privateNoteBoard != null)
                    {
                        privateNoteBoard.View++;
                        await boardRepository.UpdateBoardAsync(privateNoteBoard);
                        var attachedFile =
                            boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                        #region Encrypt img tag file path

                        HtmlDocument htmlDocument = new();
                        htmlDocument.LoadHtml(privateNoteBoard.Content);
                        var isImgTagIncluded = false;

                        foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                        {
                            isImgTagIncluded = true;
                            var filePath = imgTag.GetAttributeValue("alt", "");

                            // 파일을 다운로드하는 메서드
                            var fileData = await fileRepository.DownloadAsync(filePath);

                            if (fileData == null) continue;
                            // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                            FileExtensionContentTypeProvider provider = new();

                            // 기본 contentType은 "application/octet-stream"으로 설정
                            if (!provider.TryGetContentType(filePath, out var contentType))
                            {
                                contentType = "application/octet-stream";
                            }

                            // 파일 데이터를 Base64로 변환
                            var base64Data = Convert.ToBase64String(fileData);

                            // img 태그에 base64 데이터 저장 (src에 넣는 대신 data-file 속성 사용)
                            _ = imgTag.SetAttributeValue("data-file", base64Data);
                            _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                            _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                        }

                        privateNoteBoard.Content = htmlDocument.DocumentNode.OuterHtml;

                        #endregion

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
                                IsImgTagIncluded = isImgTagIncluded
                            };
                    }

                    if (string.IsNullOrEmpty(
                            privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ??
                            "")) return View(privateNoteOutputViewModel);
                    {
                        var fileData = await fileRepository.DownloadAsync(
                            privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                        if (fileData == null) return View(privateNoteOutputViewModel);
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(
                                privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "",
                                out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        if (privateNoteOutputViewModel.BoardOutputViewModel == null)
                            return View(privateNoteOutputViewModel);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType =
                            contentType;
                    }

                    return View(privateNoteOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("PrivateNote", "Management");
                }
            }
            // 수정 (edit)
            case "edit" when boardId == null:
                return RedirectToAction("PrivateNote", "Management");
            case "edit":
            {
                PrivateNoteOutputViewModel privateNoteOutputViewModel = new()
                {
                    Method = method,
                    EditCurrentPage = page,
                    EditBoardId = (int)boardId
                };

                _ = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);
                var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                    .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

                var privateNoteBoards =
                    await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                    .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                var boardAttachedFiles = await boardAttachedFileRepository.GetAllAsync();

                try
                {
                    var privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                    if (privateNoteBoard != null && privateNoteBoard.Writer != loginedAccount.Nickname) // 작성자와 로그인한 사용자가 같지 않으면
                    {
                        return RedirectToAction("PrivateNote", "Management");
                    }

                    var attachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);

                    // Encrypt an img tag file path
                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(privateNoteBoard?.Content ?? string.Empty);
                    var isImgTagIncluded = false;

                    foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
                    {
                        isImgTagIncluded = true;
                        var filePath = imgTag.GetAttributeValue("alt", "");

                        // 파일을 다운로드하는 메서드
                        var fileData = await fileRepository.DownloadAsync(filePath);

                        if (fileData == null) continue;
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(filePath, out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        var base64Data = Convert.ToBase64String(fileData);

                        // img 태그에 base64 데이터 저장 (src에 넣는 대신 data-file 속성 사용)
                        _ = imgTag.SetAttributeValue("data-file", base64Data);
                        _ = imgTag.SetAttributeValue("data-contenttype", contentType);
                        _ = imgTag.SetAttributeValue("alt", _rsa.Encrypt(filePath));
                    }

                    if (privateNoteBoard != null)
                    {
                        privateNoteBoard.Content = htmlDocument.DocumentNode.OuterHtml;

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
                    }

                    if (string.IsNullOrEmpty(
                            privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ??
                            "")) return View(privateNoteOutputViewModel);
                    {
                        var fileData = await fileRepository.DownloadAsync(
                            privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "");

                        if (fileData == null) return View(privateNoteOutputViewModel);
                        // ContentType을 추출하기 위한 FileExtensionContentTypeProvider 사용
                        FileExtensionContentTypeProvider provider = new();

                        // 기본 contentType은 "application/octet-stream"으로 설정
                        if (!provider.TryGetContentType(
                                privateNoteOutputViewModel.BoardOutputViewModel?.BoardAttachedFilePath ?? "",
                                out var contentType))
                        {
                            contentType = "application/octet-stream";
                        }

                        // 파일 데이터를 Base64로 변환
                        if (privateNoteOutputViewModel.BoardOutputViewModel == null)
                            return View(privateNoteOutputViewModel);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileBase64Data =
                            Convert.ToBase64String(fileData);
                        privateNoteOutputViewModel.BoardOutputViewModel.BoardAttachedFileContentType =
                            contentType;
                    }

                    return View(privateNoteOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("PrivateNote", "Management");
                }
            }
            // 목록 (list)
            default:
            {
                var isAccountSessionExist = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);
                var loginedAccount =
                    JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])); // Get Session

                #region 게시판 기본 페이징 로직

                const int pageSize = 5;
                if (page < 1)
                {
                    page = 1;
                }

                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직

                var privateNoteBoards =
                    await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                    .Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);

                #endregion

                #region 검색어 로직

                switch (searchType)
                {
                    case "Title" when !string.IsNullOrEmpty(searchText):
                    {
                        if (isAccountSessionExist) // 로그인이 되어 있다면
                        {
                            switch (loginedAccount.Role)
                            {
                                // 관리자는 모든 게시물을 볼 수 있음
                                case Role.Admin:
                                    privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText));
                                    break;
                                // 사용자는 보통 글 + 자신이 쓴 비밀글만 볼 수 있음
                                case Role.User:
                                {
                                    var tempNoteBoards = privateNoteBoards.ToList();
                                        
                                    List<Board> searchPrivateNoteBoards = [];
                                    var noLockedPrivateNoteBoards = tempNoteBoards
                                        .Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                                    var myLockedPrivateNoteBoards = tempNoteBoards
                                        .Where(a => a.Title.Contains(searchText))
                                        .Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                                    searchPrivateNoteBoards.AddRange(noLockedPrivateNoteBoards);
                                    searchPrivateNoteBoards.AddRange(myLockedPrivateNoteBoards);

                                    privateNoteBoards = searchPrivateNoteBoards;
                                    break;
                                }
                                // 로그인 시, Admin 또는 User 권한이 없다면 버그임
                            }
                        }
                        else // 로그인이 되지 않았다면
                        {
                            privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText))
                                .Where(a => !a.Locked); // 비 로그인 시, 비밀글은 검색 되지 않도록 해야함
                        }

                        break;
                    }
                    case "Writer" when !string.IsNullOrEmpty(searchText):
                        privateNoteBoards = privateNoteBoards.Where(a => a.Writer.Contains(searchText));
                        break;
                }

                #endregion

                PrivateNoteOutputViewModel privateNoteOutputViewModel = new();

                #region 공지사항

                privateNoteOutputViewModel.NoticeBoards = [];

                #endregion

                #region 게시판 목록

                privateNoteOutputViewModel.Method = "list";

                #endregion

                #region 게시판 페이징 로직

                var noteBoards = privateNoteBoards.ToList();
                privateNoteOutputViewModel.Pager = new Pager(noteBoards.Count, page, pageSize);

                #endregion

                #region 게시판 데이터 로직

                privateNoteOutputViewModel.Boards = noteBoards.Skip((page - 1) * pageSize)
                    .Take(privateNoteOutputViewModel.Pager.PageSize).ToList();

                #endregion

                ViewBag.SelectedSearchType = searchType;
                ViewBag.TypedSearchText = searchText;
                return View(privateNoteOutputViewModel);
            }
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
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            var privateNoteBoards =
                await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            {
                var tempPrivateNoteBoard = privateNoteBoards.FirstOrDefault(a => a.Id == id);

                return tempPrivateNoteBoard == null
                    ? Json(new { result = false, error = localizer["Input is invalid"].Value })
                    : (IActionResult)Json(new { result = true, PrivateNoteBoard = tempPrivateNoteBoard });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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

            if (!ModelState.IsValid) return Json(new { result = false, error = localizer["Input is invalid"].Value });
            boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

            #region HtmlSanitizer

            HtmlSanitizer sanitizer = new();
            _ = sanitizer.AllowedAttributes.Add("class");
            var sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
            boardInputViewModel.Content = sanitized;

            #endregion

            #region Decrypt img tag file path

            HtmlDocument htmlDocument = new();
            htmlDocument.LoadHtml(boardInputViewModel.Content);

            foreach (var imgTag in htmlDocument.DocumentNode.Descendants("img"))
            {
                var encryptedFilePath = imgTag.GetAttributeValue("alt", "");
                _ = imgTag.SetAttributeValue("alt", _rsa.Decrypt(encryptedFilePath));
            }

            boardInputViewModel.Content = htmlDocument.DocumentNode.OuterHtml;

            #endregion

            if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
            {
                return Json(new
                {
                    result = false,
                    error = localizer["Title length is must be between 1 and 100 characters."].Value
                });
            }

            if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
            {
                return Json(new
                {
                    result = false,
                    error = localizer["content length is must be between 0 and 16384 characters."].Value
                });
            }

            if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
            {
                if (Path.GetExtension(boardInputViewModel.UploadedFile.FileName) != ".zip") // zip 확장자만 가능
                {
                    return Json(new
                        { result = false, error = localizer["Only zip extension allowed."].Value });
                }

                if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                {
                    return Json(new
                    {
                        result = false,
                        error = localizer["uploaded file size must be smaller than 10MB."].Value
                    });
                }
            }
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var isAccountSessionExist = HttpContext.Session.TryGetValue(
                    EnumHelper.GetDescription(Session.Account), out var resultByte);

                if (!isAccountSessionExist)
                    return Json(new { result = false, error = localizer["Please Login to edit board"].Value });
                var loginedAccount =
                    JsonConvert.DeserializeObject<Account>(
                        Encoding.Default.GetString(resultByte)); // Get Session

                var privateNoteBoards =
                    await boardRepository.GetOneTypeBoardsAsync(
                        EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                    .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                var tempPrivateNoteBoard = privateNoteBoards
                    .FirstOrDefault(a => a.Id == boardInputViewModel.Id);

                if (tempPrivateNoteBoard != null && tempPrivateNoteBoard.Writer != loginedAccount.Nickname)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                if (tempPrivateNoteBoard != null)
                {
                    tempPrivateNoteBoard.Title = boardInputViewModel.Title;
                    tempPrivateNoteBoard.Content = boardInputViewModel.Content;
                    //tempPrivateNoteBoard.Locked = boardInputViewModel.Locked;
                    tempPrivateNoteBoard.Updated = DateTime.UtcNow;

                    await boardRepository.UpdateBoardAsync(tempPrivateNoteBoard);
                }

                var boardAttachedFiles =
                    await boardAttachedFileRepository?.GetAllAsync()!;
                var previousBoardAttachedFile =
                    boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                #region 첨부 파일 저장

                #region 기존 첨부 파일 O && 신 첨부 파일 O

                if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                {
                    if (boardInputViewModel.UploadedFile
                            .Length is
                        <= 0 or > 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                        return Ok(new
                        {
                            result = false,
                            errorMessage = localizer["File Size must be smaller than 10MB."].Value
                        });
                    var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                    var boardAttachedFilePath = Path.Combine("upload", "Management",
                        EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                        $"{createdBoardId}");
                    if (!Directory.Exists(boardAttachedFilePath))
                    {
                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                    }

                    var guid = Guid.NewGuid().ToString().ToUpper();
                    var attachedFile =
                        $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                    var filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                    _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                    previousBoardAttachedFile.Size =
                        Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                    previousBoardAttachedFile.Name =
                        Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                    previousBoardAttachedFile.Extension =
                        Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                    previousBoardAttachedFile.Path =
                        $"upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}";

                    await boardAttachedFileRepository?.UpdateBoardAttachedFileAsync(
                        previousBoardAttachedFile)!;
                    await transaction.CommitAsync();
                    return Json(new
                    {
                        result = true,
                        message = localizer["The board has been successfully updated."].Value
                    });

                }

                #endregion

                #region 기존 첨부 파일 O && 신 첨부 파일 X

                if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                {
                    previousBoardAttachedFile.Size = 0;
                    previousBoardAttachedFile.Name = "";
                    previousBoardAttachedFile.Extension = "";
                    previousBoardAttachedFile.Path = "";

                    await boardAttachedFileRepository?.UpdateBoardAttachedFileAsync(
                        previousBoardAttachedFile)!;
                    await transaction.CommitAsync();
                    return Json(new
                    {
                        result = true,
                        message = localizer["The board has been successfully updated."].Value
                    });
                }

                #endregion

                #region 기존 첨부 파일 X && 신 첨부 파일 O

                if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                {
                    if (boardInputViewModel.UploadedFile
                            .Length is
                        <= 0 or > 10485760) // boardInputViewModel.UploadedFile Length: 파일 크기 0MB 이상, 10MB 이하
                        return Ok(new
                        {
                            result = false,
                            errorMessage = localizer["File Size must be smaller than 10MB."].Value
                        });
                    var createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                    var boardAttachedFilePath = Path.Combine("upload", "Management",
                        EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles",
                        $"{createdBoardId}");

                    var guid = Guid.NewGuid().ToString().ToUpper();
                    var attachedFile =
                        $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                    var filePath = Path.Combine(boardAttachedFilePath, attachedFile);

                    _ = await fileRepository.UploadAsync(boardInputViewModel.UploadedFile, filePath);

                    await boardAttachedFileRepository?.SaveBoardAttachedFileAsync(
                        new BoardAttachedFile
                        {
                            BoardId = createdBoardId,
                            Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                            Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile
                                .FileName),
                            Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                            Path =
                                $"upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}"
                        })!;
                    await transaction.CommitAsync();
                    return Json(new
                    {
                        result = true,
                        message = localizer["The board has been successfully updated."].Value
                    });

                }

                #endregion

                #region 기존 첨부 파일 X && 신 첨부 파일 X

                await transaction.CommitAsync();
                return Json(new
                {
                    result = true,
                    message = localizer["The board has been successfully updated."].Value
                });

                #endregion

                #endregion

            }
            catch
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
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            var privateNoteBoards =
                await boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname)
                .Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

            {
                var tempPrivateNoteBoard =
                    privateNoteBoards.FirstOrDefault(a => a.Id == boardInputViewModel.Id);

                if (tempPrivateNoteBoard == null)
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }

                if (loginedAccount.Nickname != tempPrivateNoteBoard.Writer) // 방문한 사람이 작성자인 경우
                    return Json(new
                        { result = true, message = localizer["You do not have permission to delete."].Value });

                #region 게시물 삭제
                tempPrivateNoteBoard.Deleted = true;
                await boardRepository.DeleteBoardAsync(tempPrivateNoteBoard);
                #endregion

                return Json(new
                {
                    result = true, message = localizer["The board has been successfully deleted."].Value
                });
            }
        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
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
            var boardComment = await boardCommentRepository.GetBoardCommentAsync(id);

            if (boardComment == null)
            {
                return Json(new { result = false, error = localizer["Input is invalid"].Value });
            }

            _ = HttpContext.Session.TryGetValue(
                EnumHelper.GetDescription(Session.Account), out var resultByte);
            var loginedAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert
                .DeserializeObject<Account>(Encoding.Default.GetString(resultByte ?? [])).Email);

            if (loginedAccount.Nickname != boardComment.Writer) // 삭제하는 사람이 작성자인 경우
                return Json(new
                    { result = true, message = localizer["You do not have permission to delete."].Value });

            #region 게시물 삭제
            boardComment.Deleted = true;
            await boardCommentRepository.DeleteBoardCommentAsync(boardComment);
            #endregion

            return Json(new { result = true });

        }
        catch
        {
            return Json(new { result = false, error = localizer["Input is invalid"].Value });
        }
    }

    #endregion

    #endregion

    #endregion
}