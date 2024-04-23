using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.WebSite.Common;
using Maroik.WebSite.Helpers;
using Maroik.WebSite.Models.ViewModels.Management;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Newtonsoft.Json;
using OfficeOpenXml;
using System.Data;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace Maroik.WebSite.Controllers
{
    public class ManagementController : Controller
    {
        private readonly IHtmlLocalizer<ManagementController> localizer;
        private readonly ILogger<ManagementController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly IHostEnvironment hostEnvironment;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        public ManagementController(IHtmlLocalizer<ManagementController> localizer, ILogger<ManagementController> logger, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
        {
            this.localizer = localizer;
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
        }

        #region Profile

        #region Read
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);
            Account tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

            return View(new ProfileOutputViewModel() { Email = tempAccount.Email, AvatarImagePath = tempAccount.AvatarImagePath, Nickname = tempAccount.Nickname, Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(tempAccount.Created, OSTimeZone.DestinationTimeZoneId) });
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
            if (profileInputViewModel.ProfileAvatarFiles == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = localizer["Please attach a file"].Value });
            }
            else
            {
                IFormFile profileAvatarFile = profileInputViewModel.ProfileAvatarFiles[0];
                if (profileAvatarFile.Length is > 0 and <= 10485760) // profileAvatarFile Length : 파일 크기 0MB 이상, 10MB 이하)
                {
                    if (Path.GetExtension(profileAvatarFile.FileName) is ".png" or ".jpg" or ".jpeg")
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);
                        string avatarPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", "Avatar");
                        if (!Directory.Exists(avatarPath))
                        {
                            _ = Directory.CreateDirectory(avatarPath);
                        }

                        string guid = Guid.NewGuid().ToString().ToUpper();
                        string avatarFile = $"{guid}{Path.GetExtension(profileAvatarFile.FileName)}";
                        string filePath = Path.Combine(avatarPath, avatarFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await profileAvatarFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }

                        try
                        {
                            Account tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                            tempAccount.AvatarImagePath = $"/upload/Management/Profile/Avatar/{avatarFile}";
                            await accountRepository.UpdateAccountAsync(tempAccount);
                            return Ok(new { result = true });
                        }
                        catch
                        {
                            return Ok(new { result = false, errorMessage = localizer["Input is invalid"].Value });
                        }
                    }
                    else // 이미지 png 또는 jpg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = localizer["Only .jpg or jpeg or .png file allowed"].Value });
                    }
                }
                else // fileSizeLimitationErrorMessage
                {
                    return Ok(new { result = false, errorMessage = localizer["File Size must be smaller than 10MB."].Value });
                }
                // process uploaded files 
                // Don't rely on or trust the FileName property without validation.
            }
        }
        #endregion

        #region ProfileInformation
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[RequiredHttpPostAccess(Role = Role.Admin)]
        //[RequiredHttpPostAccess(Role = Role.User)]
        //public async Task<IActionResult> UpdateProfileInformation(ProfileInputViewModel profileInputViewModel)
        //{
        //    ModelState.Remove(nameof(profileInputViewModel.Password));
        //    ModelState.Remove(nameof(profileInputViewModel.NewPassword));

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);
        //            var tempAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
        //            tempAccount.Nickname = profileInputViewModel.Nickname;
        //            await accountRepository.UpdateAccountAsync(tempAccount);
        //        }
        //        catch
        //        {
        //            Console.WriteLine(localizer["Input is invalid"].Value);
        //        }
        //    }
        //    return RedirectToAction("Profile", "Management");
        //}
        #endregion

        #region ProfilePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateProfilePassword([FromBody] ProfileInputViewModel profileInputViewModel)
        {
            _ = ModelState.Remove(nameof(profileInputViewModel.Nickname));

            if (ModelState.IsValid)
            {
                try
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.WebSite.Common.Session.Account, out byte[] resultByte);
                    Account tempAccount = await accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if (new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Verify(profileInputViewModel.Password ?? "", tempAccount.HashedPassword)) // right password
                    {
                        tempAccount.HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(profileInputViewModel.NewPassword);
                        await accountRepository.UpdateAccountAsync(tempAccount);
                        return Json(new { result = true, message = localizer["Password change successfully done."].Value });
                    }
                    else // wrong password
                    {
                        return Json(new { result = false, error = localizer["Invalid password. Please check again."].Value });
                    }
                }
                catch
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
                    #region Email 형식 체크
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
                    if (!IsValidEmail(accountInputViewModel.Email)) // 유효하지 않은 메일 형식
                    {
                        return Json(new { result = false, error = localizer["Invalid email form"].Value });
                    }
                    #endregion

                    #region Password 형식 체크
                    if (!new Regex("^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$").IsMatch(accountInputViewModel.Password)) // 유효하지 않은 비밀번호 형식
                    {
                        return Json(new { result = false, error = localizer["Password must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)"].Value });
                    }
                    #endregion

                    #region Nickname 형식 체크
                    if (string.IsNullOrEmpty(accountInputViewModel.Nickname))
                    {
                        return Json(new { result = false, error = localizer["Please enter Nickname"].Value });
                    }
                    else if (accountInputViewModel.Nickname.Length is not (>= 1 and <= 255))
                    {
                        return Json(new { result = false, error = localizer["Nickname is must be between 1 and 255 characters"].Value });
                    }
                    #endregion

                    #region Role 체크
                    if (accountInputViewModel.Role is not (Role.Admin or Role.User))
                    {
                        accountInputViewModel.Role = Role.User;
                    }
                    #endregion

                    #region 이미 입력된 이메일로 계정이 있는 지 확인
                    if (await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email) != null)
                    {
                        return Json(new { result = false, error = localizer["This account has already been created."].Value });
                    }
                    #endregion

                    #region 계정 생성
                    await accountRepository.CreateAccountAsync
                    (
                    new Account()
                    {
                        Email = accountInputViewModel.Email,
                        HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(accountInputViewModel.Password), // 암호화
                        Nickname = accountInputViewModel.Nickname,
                        AvatarImagePath = "/upload/Management/Profile/default-avatar.jpg",
                        Role = accountInputViewModel.Role,
                        Locked = false,
                        LoginAttempt = 0,
                        EmailConfirmed = true,
                        AgreedServiceTerms = true,
                        RegistrationToken = null,
                        ResetPasswordToken = null,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                        Message = AccountMessage.Success,
                        Deleted = false
                    }
                    );
                    return Json(new { result = true, message = localizer["Account create successfully done."].Value });
                    #endregion
                }
                else
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

        #region Read
        [HttpGet]
        public async Task<IActionResult> Account(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<AccountOutputViewModel> accountOutputViewModels = [];

                #region Accounts
                foreach (Account item in await accountRepository.GetAllAsync())
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
                            Locked = item.Locked,
                            LoginAttempt = item.LoginAttempt,
                            EmailConfirmed = item.EmailConfirmed,
                            AgreedServiceTerms = item.AgreedServiceTerms,
                            RegistrationToken = item.RegistrationToken,
                            ResetPasswordToken = item.ResetPasswordToken,
                            Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                            Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
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
                            (item.Locked.ToString() ?? "").Contains(wholeSearch) ||
                            (item.LoginAttempt.ToString() ?? "").Contains(wholeSearch) ||
                            (item.EmailConfirmed.ToString() ?? "").Contains(wholeSearch) ||
                            (item.AgreedServiceTerms.ToString() ?? "").Contains(wholeSearch) ||
                            (item.RegistrationToken?.ToString() ?? "").Contains(wholeSearch) ||
                            (item.ResetPasswordToken?.ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
                            (TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId).ToString() ?? "").Contains(wholeSearch) ||
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
                                Locked = item.Locked,
                                LoginAttempt = item.LoginAttempt,
                                EmailConfirmed = item.EmailConfirmed,
                                AgreedServiceTerms = item.AgreedServiceTerms,
                                RegistrationToken = item.RegistrationToken,
                                ResetPasswordToken = item.ResetPasswordToken,
                                Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                                Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                                Message = item.Message,
                                Deleted = item.Deleted
                            });
                        }
                    }
                }
                #endregion

                IQueryable<AccountOutputViewModel> result = accountOutputViewModels.OrderBy(m => m.Email).ThenBy(m => m.Nickname).ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated).AsQueryable();
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
                Account tempAccount = await accountRepository.GetAccountByEmailAsync(email);

                return tempAccount == null
                    ? Json(new { result = false, error = localizer["Fail to find the account by given email address"].Value })
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

                if (ModelState.IsValid)
                {
                    Account tempAccount = await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

                    if (tempAccount == null)
                    {
                        return Json(new { result = false, error = localizer["Email address is wrong"].Value });
                    }

                    tempAccount.Email = accountInputViewModel.Email;
                    tempAccount.HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(accountInputViewModel.Password); // 암호화
                    tempAccount.Role = accountInputViewModel.Role;
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
                else
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

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public async Task<IActionResult> DeleteAccount([FromBody] AccountInputViewModel accountInputViewModel)
        {
            try
            {
                Account tempAccount = await accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

                if (tempAccount == null)
                {
                    return Json(new { result = false, error = localizer["Fail to find the account by given email address"].Value });
                }
                else
                {
                    tempAccount.Deleted = true;
                    await accountRepository.UpdateAccountAsync(tempAccount);
                    return Json(new { result = true, message = localizer["Successfully deleted the account"].Value });
                }
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

            // query data from database
            await Task.Yield();

            List<AccountOutputViewModel> accountOutputViewModels = [];

            #region Accounts
            foreach (Account item in await accountRepository.GetAllAsync())
            {
                accountOutputViewModels.Add(new AccountOutputViewModel()
                {
                    Email = item.Email,
                    HashedPassword = item.HashedPassword,
                    Nickname = item.Nickname,
                    AvatarImagePath = item.AvatarImagePath,
                    Role = item.Role,
                    Locked = item.Locked,
                    LoginAttempt = item.LoginAttempt,
                    EmailConfirmed = item.EmailConfirmed,
                    AgreedServiceTerms = item.AgreedServiceTerms,
                    RegistrationToken = item.RegistrationToken,
                    ResetPasswordToken = item.ResetPasswordToken,
                    Created = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Created, OSTimeZone.DestinationTimeZoneId),
                    Updated = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(item.Updated, OSTimeZone.DestinationTimeZoneId),
                    Message = item.Message,
                    Deleted = item.Deleted
                });
            }
            accountOutputViewModels = accountOutputViewModels.OrderBy(m => m.Email).ThenBy(m => m.Nickname).ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();
            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(AccountOutputViewModel.Email).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(AccountOutputViewModel.HashedPassword).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(AccountOutputViewModel.Nickname).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(AccountOutputViewModel.AvatarImagePath).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(AccountOutputViewModel.Role).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(AccountOutputViewModel.Locked).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(AccountOutputViewModel.LoginAttempt).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(AccountOutputViewModel.EmailConfirmed).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(AccountOutputViewModel.AgreedServiceTerms).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 10].Value = localizer[nameof(AccountOutputViewModel.RegistrationToken).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 11].Value = localizer[nameof(AccountOutputViewModel.ResetPasswordToken).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 12].Value = localizer[nameof(AccountOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 13].Value = localizer[nameof(AccountOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 14].Value = localizer[nameof(AccountOutputViewModel.Message).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 15].Value = localizer[nameof(AccountOutputViewModel.Deleted).ToString()].Value;

                int recordIndex = 2;
                foreach (AccountOutputViewModel item in accountOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.Email;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = item.HashedPassword;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Nickname;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.AvatarImagePath;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Role;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Locked;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.LoginAttempt;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.EmailConfirmed;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.AgreedServiceTerms;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 10].Value = item.RegistrationToken;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 11].Value = item.ResetPasswordToken;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 12].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 13].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 14].Value = item.Message;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 15].Value = item.Deleted;
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

        #region Menu

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public async Task<IActionResult> CreateCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                _ = ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
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
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
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
                _ = ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
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

                    return Json(new { result = true, message = localizer["Successfully created the subCategory"].Value });
                }
                else
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

        #region Read
        [HttpGet]
        public async Task<IActionResult> Menu(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<MenuOutputViewModel> menuOutputViewModels = [];

                #region AdminCategories
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.User))
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
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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

                IQueryable<MenuOutputViewModel> result = menuOutputViewModels.OrderByDescending(m => m.Id).ThenByDescending(m => m.CategoryId).AsQueryable();
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
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.User))
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
                foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
                    throw new Exception(); // DB에서 못 찾음
                }
                return Json(new { result = true, category = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
                foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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
                    throw new Exception(); // DB에서 못 찾음
                }
                return Json(new { result = true, subCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
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
                _ = ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
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
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
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
                _ = ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
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

                    return Json(new { result = true, message = localizer["Successfully updated the subCategory"].Value });
                }
                else
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

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public async Task<IActionResult> DeleteCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                _ = ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
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
                else
                {
                    return Json(new { result = false, error = localizer["Input is invalid"].Value });
                }
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
                _ = ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
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

                    return Json(new { result = true, message = localizer["Successfully deleted the subCategory"].Value });
                }
                else
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

        #region Excel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public async Task<IActionResult> ExportExcelMenu(string fileName = "")
        {

            // query data from database
            await Task.Yield();

            List<MenuOutputViewModel> menuOutputViewModels = [];

            #region AdminCategories
            foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
            foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.User))
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
            foreach (Category item in await categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
            foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
            foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
            foreach (SubCategory item in await subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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

            menuOutputViewModels = menuOutputViewModels.OrderByDescending(m => m.Id).ThenByDescending(m => m.CategoryId).ToList();

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = localizer[nameof(MenuOutputViewModel.Id).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = localizer[nameof(MenuOutputViewModel.CategoryId).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = localizer[nameof(MenuOutputViewModel.Name).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = localizer[nameof(MenuOutputViewModel.DisplayName).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = localizer[nameof(MenuOutputViewModel.IconPath).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = localizer[nameof(MenuOutputViewModel.Controller).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = localizer[nameof(MenuOutputViewModel.Action).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = localizer[nameof(MenuOutputViewModel.Role).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = localizer[nameof(MenuOutputViewModel.Order).ToString()].Value;

                int recordIndex = 2;
                foreach (MenuOutputViewModel item in menuOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.Id;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = item.CategoryId;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Name;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.DisplayName;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.IconPath;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.Controller;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Action;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.Role;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.Order;
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