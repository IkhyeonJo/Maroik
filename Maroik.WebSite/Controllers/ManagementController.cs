using Ganss.Xss;
using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.Miscellaneous.Extensions;
using Maroik.Common.Miscellaneous.Utilities;
using Maroik.WebSite.Models;
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
        private readonly IHtmlLocalizer<ManagementController> _localizer;
        private readonly ILogger<ManagementController> _logger;
        private readonly IAccountRepository _accountRepository;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ISubCategoryRepository _subCategoryRepository;
        private readonly IBoardRepository _boardRepository;
        private readonly IBoardAttachedFileRepository _boardAttachedFileRepository;
        private readonly IBoardCommentRepository _boardCommentRepository;

        public ManagementController(IHtmlLocalizer<ManagementController> localizer, ILogger<ManagementController> logger, IBoardRepository boardRepository, IBoardAttachedFileRepository boardAttachedFileRepository, IBoardCommentRepository boardCommentRepository, IAccountRepository accountRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
        {
            _localizer = localizer;
            _logger = logger;
            _boardRepository = boardRepository;
            _boardAttachedFileRepository = boardAttachedFileRepository;
            _accountRepository = accountRepository;
            _hostEnvironment = hostEnvironment;
            _categoryRepository = categoryRepository;
            _subCategoryRepository = subCategoryRepository;
            _boardCommentRepository = boardCommentRepository;
        }

        #region Profile

        #region Read
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;
            Account tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

            return View(new ProfileOutputViewModel() { Email = tempAccount.Email, AvatarImagePath = tempAccount.AvatarImagePath, Nickname = tempAccount.Nickname, Created = tempAccount.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId), TimeZoneIanaId = tempAccount.TimeZoneIanaId });
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
                return Ok(new { result = false, errorMessage = _localizer["Please attach a file"].Value });
            }
            else
            {
                IFormFile profileAvatarFile = profileInputViewModel.ProfileAvatarFiles[0];
                if (profileAvatarFile.Length is > 0 and <= 10485760) // profileAvatarFile Length : 파일 크기 0MB 이상, 10MB 이하)
                {
                    if (Path.GetExtension(profileAvatarFile.FileName) is ".png" or ".jpg" or ".jpeg")
                    {
                        _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                        string avatarPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", "Avatar");
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
                            Account tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                            tempAccount.AvatarImagePath = $"/upload/Management/Profile/Avatar/{avatarFile}";
                            await _accountRepository.UpdateAccountAsync(tempAccount);
                            return Ok(new { result = true });
                        }
                        catch
                        {
                            return Ok(new { result = false, errorMessage = _localizer["Input is invalid"].Value });
                        }
                    }
                    else // 이미지 png 또는 jpg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed"].Value });
                    }
                }
                else // fileSizeLimitationErrorMessage
                {
                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                }
                // process uploaded files 
                // Don't rely on or trust the FileName property without validation.
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
                    _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                    Account tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                    tempAccount.TimeZoneIanaId = profileInputViewModel.TimeZoneIanaId;
                    await _accountRepository.UpdateAccountAsync(tempAccount);

                    HttpContext.Session.Remove(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account));
                    tempAccount.HashedPassword = null;
                    HttpContext.Session.Set(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(tempAccount)));
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

            if (ModelState.IsValid)
            {
                try
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                    Account tempAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if (new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Verify(profileInputViewModel.Password ?? "", tempAccount.HashedPassword)) // right password
                    {
                        tempAccount.HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(profileInputViewModel.NewPassword);
                        await _accountRepository.UpdateAccountAsync(tempAccount);
                        return Json(new { result = true, message = _localizer["Password change successfully done."].Value });
                    }
                    else // wrong password
                    {
                        return Json(new { result = false, error = _localizer["Invalid password. Please check again."].Value });
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
                        return Json(new { result = false, error = _localizer["Invalid email form"].Value });
                    }
                    #endregion

                    #region Password 형식 체크
                    if (!new Regex("^((?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])|(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[^a-zA-Z0-9])|(?=.*?[A-Z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])|(?=.*?[a-z])(?=.*?[0-9])(?=.*?[^a-zA-Z0-9])).{8,}$").IsMatch(accountInputViewModel.Password)) // 유효하지 않은 비밀번호 형식
                    {
                        return Json(new { result = false, error = _localizer["Password must be at least 8 characters and contain at 3 of 4 of the following: upper case (A-Z), lower case (a-z), number (0-9) and special character (e.g. !@#$%^&*)"].Value });
                    }
                    #endregion

                    #region Nickname 형식 체크
                    if (string.IsNullOrEmpty(accountInputViewModel.Nickname))
                    {
                        return Json(new { result = false, error = _localizer["Please enter Nickname"].Value });
                    }
                    else if (accountInputViewModel.Nickname.Length is not (>= 1 and <= 255))
                    {
                        return Json(new { result = false, error = _localizer["Nickname is must be between 1 and 255 characters"].Value });
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
                        return Json(new { result = false, error = _localizer["Please select time zone"].Value });
                    }
                    #endregion

                    #region 이미 입력된 이메일로 계정이 있는 지 확인
                    if (await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email) != null)
                    {
                        return Json(new { result = false, error = _localizer["This account has already been created."].Value });
                    }
                    #endregion

                    #region 계정 생성
                    await _accountRepository.CreateAccountAsync
                    (
                    new Account()
                    {
                        Email = accountInputViewModel.Email,
                        HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(accountInputViewModel.Password), // 암호화
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
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

                #region Accounts
                foreach (Account item in await _accountRepository.GetAllAsync())
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
                            (item.Created.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch) ||
                            (item.Updated.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId).ToString() ?? "").Contains(wholeSearch) ||
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
                Account tempAccount = await _accountRepository.GetAccountByEmailAsync(email);

                return tempAccount == null
                    ? Json(new { result = false, error = _localizer["Fail to find the account by given email address"].Value })
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
                    Account tempAccount = await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

                    if (tempAccount == null)
                    {
                        return Json(new { result = false, error = _localizer["Email address is wrong"].Value });
                    }

                    tempAccount.Email = accountInputViewModel.Email;
                    tempAccount.HashedPassword = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey).Sign(accountInputViewModel.Password); // 암호화
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

                    await _accountRepository.UpdateAccountAsync(tempAccount);

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
                Account tempAccount = await _accountRepository.GetAccountByEmailAsync(accountInputViewModel.Email);

                if (tempAccount == null)
                {
                    return Json(new { result = false, error = _localizer["Fail to find the account by given email address"].Value });
                }
                else
                {
                    tempAccount.Deleted = true;
                    await _accountRepository.UpdateAccountAsync(tempAccount);
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

            // query data from database
            await Task.Yield();

            List<AccountOutputViewModel> accountOutputViewModels = [];
            _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            #region Accounts
            foreach (Account item in await _accountRepository.GetAllAsync())
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
            accountOutputViewModels = accountOutputViewModels.OrderBy(m => m.Email).ThenBy(m => m.Nickname).ThenByDescending(m => m.Created).ThenByDescending(m => m.Updated).ToList();
            #endregion

            MemoryStream stream = new();

            using (ExcelPackage package = new(stream))
            {
                ExcelWorksheet workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");

                workSheetMenuOutputViewModels.Row(1).Height = 20;
                workSheetMenuOutputViewModels.Row(1).Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                workSheetMenuOutputViewModels.Row(1).Style.Font.Bold = true;
                workSheetMenuOutputViewModels.Cells[1, 1].Value = _localizer[nameof(AccountOutputViewModel.Email).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = _localizer[nameof(AccountOutputViewModel.HashedPassword).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = _localizer[nameof(AccountOutputViewModel.Nickname).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = _localizer[nameof(AccountOutputViewModel.AvatarImagePath).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = _localizer[nameof(AccountOutputViewModel.Role).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = _localizer[nameof(AccountOutputViewModel.TimeZoneIanaId).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = _localizer[nameof(AccountOutputViewModel.Locked).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = _localizer[nameof(AccountOutputViewModel.LoginAttempt).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = _localizer[nameof(AccountOutputViewModel.EmailConfirmed).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 10].Value = _localizer[nameof(AccountOutputViewModel.AgreedServiceTerms).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 11].Value = _localizer[nameof(AccountOutputViewModel.RegistrationToken).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 12].Value = _localizer[nameof(AccountOutputViewModel.ResetPasswordToken).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 13].Value = _localizer[nameof(AccountOutputViewModel.Created).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 14].Value = _localizer[nameof(AccountOutputViewModel.Updated).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 15].Value = _localizer[nameof(AccountOutputViewModel.Message).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 16].Value = _localizer[nameof(AccountOutputViewModel.Deleted).ToString()].Value;

                int recordIndex = 2;
                foreach (AccountOutputViewModel item in accountOutputViewModels)
                {
                    workSheetMenuOutputViewModels.Cells[recordIndex, 1].Value = item.Email;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 2].Value = item.HashedPassword;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 3].Value = item.Nickname;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 4].Value = item.AvatarImagePath;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 5].Value = item.Role;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 6].Value = item.TimeZoneIanaId;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 7].Value = item.Locked;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 8].Value = item.LoginAttempt;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 9].Value = item.EmailConfirmed;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 10].Value = item.AgreedServiceTerms;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 11].Value = item.RegistrationToken;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 12].Value = item.ResetPasswordToken;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 13].Value = item.Created.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 14].Value = item.Updated.ToString();
                    workSheetMenuOutputViewModels.Cells[recordIndex, 15].Value = item.Message;
                    workSheetMenuOutputViewModels.Cells[recordIndex, 16].Value = item.Deleted;
                    recordIndex++;
                }

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

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
                    await _categoryRepository.CreateCategoryAsync(category);

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
                    await _subCategoryRepository.CreateSubCategoryAsync(subCategory);

                    return Json(new { result = true, message = _localizer["Successfully created the subCategory"].Value });
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
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
                foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
                foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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
                    await _categoryRepository.UpdateCategoryAsync(category);

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
                    await _subCategoryRepository.UpdateSubCategoryAsync(subCategory);

                    return Json(new { result = true, message = _localizer["Successfully updated the subCategory"].Value });
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
                    await _categoryRepository.DeleteCategoryAsync(category);

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
                    await _subCategoryRepository.DeleteSubCategoryAsync(subCategory);

                    return Json(new { result = true, message = _localizer["Successfully deleted the subCategory"].Value });
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

            // query data from database
            await Task.Yield();

            List<MenuOutputViewModel> menuOutputViewModels = [];
            _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
            string loginedAccountTimeZoneIanaId = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).TimeZoneIanaId;

            #region AdminCategories
            foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Admin))
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
            foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.User))
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
            foreach (Category item in await _categoryRepository.GetCategoryByRoleAsync(Role.Anonymous))
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
            foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Admin))
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
            foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.User))
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
            foreach (SubCategory item in await _subCategoryRepository.GetSubCategoryByRoleAsync(Role.Anonymous))
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
                workSheetMenuOutputViewModels.Cells[1, 1].Value = _localizer[nameof(MenuOutputViewModel.Id).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 2].Value = _localizer[nameof(MenuOutputViewModel.CategoryId).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 3].Value = _localizer[nameof(MenuOutputViewModel.Name).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 4].Value = _localizer[nameof(MenuOutputViewModel.DisplayName).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 5].Value = _localizer[nameof(MenuOutputViewModel.IconPath).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 6].Value = _localizer[nameof(MenuOutputViewModel.Controller).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 7].Value = _localizer[nameof(MenuOutputViewModel.Action).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 8].Value = _localizer[nameof(MenuOutputViewModel.Role).ToString()].Value;
                workSheetMenuOutputViewModels.Cells[1, 9].Value = _localizer[nameof(MenuOutputViewModel.Order).ToString()].Value;

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
            string excelName = $"{fileName}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(loginedAccountTimeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";

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

                    if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                        }

                        if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session
                            Board board = new();

                            if (loginedAccount.Role == Role.Admin) // 관리자인 경우
                            {
                                board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // 사적 메모
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.Views = 0;
                                board.Deleted = false;
                                //board.Locked = boardInputViewModel.Locked;
                                board.Noticed = boardInputViewModel.Noticed;
                            }
                            else if (loginedAccount.Role == Role.User) // 일반 사용자인 경우
                            {
                                board.Type = EnumHelper.GetDescription(BoardType.PrivateNote); // 사적 메모
                                board.Title = boardInputViewModel.Title;
                                board.Content = boardInputViewModel.Content;
                                board.Writer = loginedAccount.Nickname;
                                board.Created = DateTime.UtcNow;
                                board.Updated = DateTime.UtcNow;
                                board.Views = 0;
                                board.Deleted = false;
                                //board.Locked = boardInputViewModel.Locked;
                            }
                            else // 비로그인 사용자인 경우
                            {
                                return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
                            }

                            await _boardRepository.WriteBoardAsync(board);

                            #region 첨부 파일 저장
                            if (boardInputViewModel.UploadedFile != null) // 첨부 파일 존재 시,
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = board.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}"
                                    });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }

                            }
                            #endregion

                            return Json(new { result = true, message = _localizer["The board has been successfully created."].Value });
                        }
                        else
                        {
                            return Json(new { result = false, error = _localizer["Please Login to write board"].Value });
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

        #region PrivateNoteComment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> WritePrivateNoteComment([FromBody] BoardCommentInputViewModel boardCommentInputViewModel)
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
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                            Board privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardCommentInputViewModel.BoardId);

                            if (privateNoteBoard.Id != boardCommentInputViewModel.BoardId)
                            {
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            IEnumerable<BoardComment> totalBoardCommentsInSelectedBoard = await _boardCommentRepository.GetOneBoardCommentsAsync(boardCommentInputViewModel.BoardId);

                            BoardComment boardComment = new()
                            {
                                BoardId = boardCommentInputViewModel.BoardId,
                                Order = totalBoardCommentsInSelectedBoard?.Count() ?? default,
                                AvatarImagePath = loginedAccount.AvatarImagePath,
                                Writer = loginedAccount.Nickname,
                                Content = boardCommentInputViewModel.Content,
                                Created = DateTime.UtcNow,
                                Deleted = false
                            };
                            await _boardCommentRepository.WriteBoardCommentAsync(boardComment);

                            return Json(new { result = true, boardId = boardCommentInputViewModel.BoardId, page = boardCommentInputViewModel.DetailCurrentPage });
                        }
                        else
                        {
                            return Json(new { result = false, error = _localizer["Please Login to write comment."].Value });
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
            if (summernoteImageFile == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = _localizer["Please attach a file."].Value });
            }
            else
            {
                if (summernoteImageFile.Length is > 0 and <= 10485760) // imageFile Length : 파일 크기 0MB 이상, 10MB 이하
                {
                    if (Path.GetExtension(summernoteImageFile.FileName) is ".png" or ".jpg" or ".jpeg")
                    {

                        string summernoteImagePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", EnumHelper.GetDescription(BoardType.PrivateNote), "summernote", "images");
                        if (!Directory.Exists(summernoteImagePath))
                        {
                            _ = Directory.CreateDirectory(summernoteImagePath);
                        }

                        string imageFile = $"{Guid.NewGuid().ToString().ToUpper()}{Path.GetExtension(summernoteImageFile.FileName)}";
                        string filePath = Path.Combine(summernoteImagePath, imageFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await summernoteImageFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }
                        return Ok(new { result = true, filePath = $"/upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/summernote/images/{imageFile}" });
                    }
                    else // 이미지 png 또는 jpg 또는 jpeg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = _localizer["Only .jpg or jpeg or .png file allowed."].Value });
                    }
                }
                else
                {
                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                }
            }
        }
        #endregion

        #endregion

        #region Read

        #region Edit, List, Detail, Write
        public async Task<IActionResult> PrivateNote(string method = "list", int? boardId = null, int page = 1, string searchType = "", string searchText = "")
        {
            if (method == "write") // 글쓰기 (write)
            {
                PrivateNoteOutputViewModel privateNoteOutputViewModel = new();

                #region 게시판 글쓰기
                privateNoteOutputViewModel.Method = method;
                #endregion

                bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out _); // 로그인이 되어 계정 세션이 생겼는지 확인

                return isAccountSessionExist ? View(privateNoteOutputViewModel) : RedirectToAction("Login", "Account");
            }
            else if (method == "detail") // 상세보기 (detail)
            {
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte); // 작성자 또는 관리자인 경우
                Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

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

                IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

                try
                {
                    Board privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                    if (privateNoteBoard.Locked)
                    {
                        if (!(loginedAccount.Nickname == privateNoteBoard.Writer || loginedAccount.Role == Role.Admin))
                        {
                            return RedirectToAction("PrivateNote", "Management");
                        }
                    }

                    privateNoteBoard.Views++;
                    await _boardRepository.UpdateBoardAsync(privateNoteBoard);
                    BoardAttachedFile attachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    privateNoteOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = privateNoteBoard.Title,
                        Writer = privateNoteBoard.Writer,
                        Views = privateNoteBoard.Views,
                        Content = privateNoteBoard.Content,
                        Updated = privateNoteBoard.Updated,
                        BoardAttachedFileName = attachedFile?.Name ?? "",
                        BoardAttachedFileExtension = attachedFile?.Extension ?? "",
                        BoardAttachedFileSize = attachedFile?.Size ?? 0,
                        BoardAttachedFilePath = attachedFile?.Path ?? ""
                    };

                    return View(privateNoteOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("PrivateNote", "Management");
                }
            }
            else if (method == "edit") // 수정 (edit)
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

                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);
                List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository.GetAllAsync();

                try
                {
                    Board privateNoteBoard = privateNoteBoards.FirstOrDefault(m => m.Id == boardId);

                    if (privateNoteBoard.Writer != loginedAccount.Nickname) // 작성자와 로그인한 사용자가 같지 않으면
                    {
                        return RedirectToAction("PrivateNote", "Management");
                    }

                    BoardAttachedFile attachedFile = boardAttachedFiles.FirstOrDefault(m => m.BoardId == boardId);


                    privateNoteOutputViewModel.BoardOutputViewModel = new BoardOutputViewModel()
                    {
                        Title = privateNoteBoard.Title,
                        Writer = privateNoteBoard.Writer,
                        Views = privateNoteBoard.Views,
                        Content = privateNoteBoard.Content,
                        Locked = privateNoteBoard.Locked,
                        Updated = privateNoteBoard.Updated,
                        BoardAttachedFileName = attachedFile?.Name ?? "",
                        BoardAttachedFileExtension = attachedFile?.Extension ?? "",
                        BoardAttachedFileSize = attachedFile?.Size ?? 0,
                        BoardAttachedFilePath = attachedFile?.Path ?? ""
                    };

                    return View(privateNoteOutputViewModel);
                }
                catch
                {
                    return RedirectToAction("PrivateNote", "Management");
                }

            }
            else // 목록 (list)
            {
                bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                #region 게시판 기본 페이징 로직
                const int pageSize = 5;
                if (page < 1)
                {
                    page = 1;
                }
                #endregion

                #region 관련된 모든 게시판 데이터 가져오는 로직
                IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false && m.Noticed == false).OrderByDescending(a => a.Id);
                #endregion

                #region 검색어 로직
                if (searchType == "Title" && !string.IsNullOrEmpty(searchText))
                {
                    if (isAccountSessionExist) // 로그인이 되어 있다면
                    {
                        if (loginedAccount.Role == Role.Admin) // 관리자는 모든 게시물을 볼 수 있음
                        {
                            privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText));
                        }
                        else if (loginedAccount.Role == Role.User) // 사용자는 보통 글 + 자신이 쓴 비밀글만 볼 수 있음
                        {
                            List<Board> searchPrivateNoteBoards = [];
                            List<Board> noLockedPrivateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked).ToList();
                            List<Board> myLockedPrivateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText)).Where(a => a.Locked && a.Writer == loginedAccount.Nickname).ToList();
                            searchPrivateNoteBoards.AddRange(noLockedPrivateNoteBoards);
                            searchPrivateNoteBoards.AddRange(myLockedPrivateNoteBoards);

                            privateNoteBoards = searchPrivateNoteBoards;
                        }
                        else // 로그인 시, Admin 또는 User 권한이 없다면 버그임
                        {

                        }
                    }
                    else // 로그인이 되지 않았다면
                    {
                        privateNoteBoards = privateNoteBoards.Where(a => a.Title.Contains(searchText)).Where(a => !a.Locked); // 비 로그인 시, 비밀글은 검색 되지 않도록 해야함
                    }
                }
                else if (searchType == "Writer" && !string.IsNullOrEmpty(searchText))
                {
                    privateNoteBoards = privateNoteBoards.Where(a => a.Writer.Contains(searchText));
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
                privateNoteOutputViewModel.Pager = new Pager(privateNoteBoards.Count(), page, pageSize);
                #endregion

                #region 게시판 데이터 로직
                privateNoteOutputViewModel.Boards = privateNoteBoards.Skip((page - 1) * pageSize).Take(privateNoteOutputViewModel.Pager.PageSize).ToList();
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
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                if (privateNoteBoards == null)
                {
                    return Json(new { result = false, error = _localizer["No PrivateNoteBoard exists"].Value });
                }
                else
                {
                    Board tempPrivateNoteBoard = privateNoteBoards.Where(a => a.Id == id).FirstOrDefault();

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
                    boardInputViewModel.Content ??= ""; // 내용이 아무것도 입력되지 않았다면

                    #region HtmlSanitizer
                    HtmlSanitizer sanitizer = new();
                    _ = sanitizer.AllowedAttributes.Add("class");
                    string sanitized = sanitizer.Sanitize(boardInputViewModel.Content);
                    boardInputViewModel.Content = sanitized;
                    #endregion

                    if (boardInputViewModel.Title.Length is not (> 0 and <= 100)) // 제목 길이
                    {
                        return Json(new { result = false, error = _localizer["Title length is must be between 1 and 100 characters."].Value });
                    }

                    if (boardInputViewModel.Content.Length > 16384) // Content Maxlength: 16KB
                    {
                        return Json(new { result = false, error = _localizer["content length is must be between 0 and 16384 characters."].Value });
                    }

                    if (boardInputViewModel.UploadedFile != null) // 파일 첨부가 되었다면
                    {
                        if (!(Path.GetExtension(boardInputViewModel.UploadedFile.FileName) == ".zip")) // zip 확장자만 가능
                        {
                            return Json(new { result = false, error = _localizer["Only zip extension allowed."].Value });
                        }

                        if (boardInputViewModel.UploadedFile.Length is not (> 0 and <= 10485760)) // 10MB 이하만 가능
                        {
                            return Json(new { result = false, error = _localizer["uploaded file size must be smaller than 10MB."].Value });
                        }
                    }

                    try
                    {
                        bool isAccountSessionExist = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);

                        if (isAccountSessionExist)
                        {
                            Account loginedAccount = JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)); // Get Session

                            IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                            privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                            Board tempPrivateNoteBoard = privateNoteBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                            if (tempPrivateNoteBoard.Writer != loginedAccount.Nickname)
                            {
                                return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                            }

                            tempPrivateNoteBoard.Title = boardInputViewModel.Title;
                            tempPrivateNoteBoard.Content = boardInputViewModel.Content;
                            //tempPrivateNoteBoard.Locked = boardInputViewModel.Locked;
                            tempPrivateNoteBoard.Updated = DateTime.UtcNow;

                            await _boardRepository.UpdateBoardAsync(tempPrivateNoteBoard);

                            List<BoardAttachedFile> boardAttachedFiles = await _boardAttachedFileRepository?.GetAllAsync();
                            BoardAttachedFile previousBoardAttachedFile = boardAttachedFiles?.FirstOrDefault(m => m.BoardId == boardInputViewModel.Id);

                            #region 첨부 파일 저장

                            #region 기존 첨부 파일 O && 신 첨부 파일 O
                            if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    previousBoardAttachedFile.Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length);
                                    previousBoardAttachedFile.Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousBoardAttachedFile.Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName);
                                    previousBoardAttachedFile.Path = $"/upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}";

                                    await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);

                                    return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 O && 신 첨부 파일 X
                            else if (previousBoardAttachedFile != null && boardInputViewModel.UploadedFile == null)
                            {
                                previousBoardAttachedFile.Size = 0;
                                previousBoardAttachedFile.Name = "";
                                previousBoardAttachedFile.Extension = "";
                                previousBoardAttachedFile.Path = "";

                                await _boardAttachedFileRepository.UpdateBoardAttachedFileAsync(previousBoardAttachedFile);

                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 O
                            else if (previousBoardAttachedFile == null && boardInputViewModel.UploadedFile != null)
                            {
                                if (boardInputViewModel.UploadedFile.Length is > 0 and <= 10485760) // boardInputViewModel.UploadedFile Length : 파일 크기 0MB 이상, 10MB 이하
                                {
                                    int createdBoardId = boardInputViewModel.Id; // 첨부파일 FK 해서 로직 추가할 것

                                    string boardAttachedFilePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", EnumHelper.GetDescription(BoardType.PrivateNote), "boardAttachedFiles", $"{createdBoardId}");
                                    if (!Directory.Exists(boardAttachedFilePath))
                                    {
                                        _ = Directory.CreateDirectory(boardAttachedFilePath);
                                    }

                                    string guid = Guid.NewGuid().ToString().ToUpper();
                                    string attachedFile = $"{guid}{Path.GetExtension(boardInputViewModel.UploadedFile.FileName)}";
                                    string filePath = Path.Combine(boardAttachedFilePath, attachedFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    using (FileStream stream = new(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                                    {
                                        await boardInputViewModel.UploadedFile.CopyToAsync(stream);
                                        await stream.FlushAsync();
                                    }

                                    await _boardAttachedFileRepository.SaveBoardAttachedFileAsync(new BoardAttachedFile()
                                    {
                                        BoardId = createdBoardId,
                                        Size = Convert.ToInt32(boardInputViewModel.UploadedFile.Length),
                                        Name = Path.GetFileNameWithoutExtension(boardInputViewModel.UploadedFile.FileName),
                                        Extension = Path.GetExtension(boardInputViewModel.UploadedFile.FileName),
                                        Path = $"/upload/Management/{EnumHelper.GetDescription(BoardType.PrivateNote)}/boardAttachedFiles/{createdBoardId}/{attachedFile}"
                                    });

                                    return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                                }
                                else
                                {
                                    return Ok(new { result = false, errorMessage = _localizer["File Size must be smaller than 10MB."].Value });
                                }
                            }
                            #endregion

                            #region 기존 첨부 파일 X && 신 첨부 파일 X
                            else
                            {
                                return Json(new { result = true, message = _localizer["The board has been successfully updated."].Value });
                            }
                            #endregion

                            #endregion
                        }
                        else
                        {
                            return Json(new { result = false, error = _localizer["Please Login to edit board"].Value });
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
                _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                IEnumerable<Board> privateNoteBoards = await _boardRepository.GetOneTypeBoardsAsync(EnumHelper.GetDescription(BoardType.PrivateNote));
                privateNoteBoards = privateNoteBoards.Where(m => m.Writer == loginedAccount.Nickname).Where(m => m.Deleted == false).OrderByDescending(a => a.Id);

                if (privateNoteBoards == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    Board tempPrivateNoteBoard = privateNoteBoards.Where(a => a.Id == boardInputViewModel.Id).FirstOrDefault();

                    if (tempPrivateNoteBoard == null)
                    {
                        return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                    }
                    else
                    {
                        if (loginedAccount.Nickname == tempPrivateNoteBoard.Writer) // 방문한 사람이 작성자인 경우
                        {
                            #region 게시물 삭제
                            tempPrivateNoteBoard.Deleted = true;
                            await _boardRepository.DeleteBoardAsync(tempPrivateNoteBoard);
                            #endregion

                            return Json(new { result = true, message = _localizer["The board has been successfully deleted."].Value });
                        }
                        else
                        {
                            return Json(new { result = true, message = _localizer["You do not have permission to delete."].Value });
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
                BoardComment boardComment = await _boardCommentRepository.GetBoardCommentAsync(id);

                if (boardComment == null)
                {
                    return Json(new { result = false, error = _localizer["Input is invalid"].Value });
                }
                else
                {
                    _ = HttpContext.Session.TryGetValue(Maroik.Common.Miscellaneous.Extensions.EnumHelper.GetDescription(Maroik.Common.Miscellaneous.Utilities.Session.Account), out byte[] resultByte);
                    Account loginedAccount = await _accountRepository.GetAccountByEmailAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

                    if (loginedAccount.Nickname == boardComment.Writer) // 삭제하는 사람이 작성자인 경우
                    {
                        #region 게시물 삭제
                        boardComment.Deleted = true;
                        await _boardCommentRepository.DeleteBoardCommentAsync(boardComment);
                        #endregion

                        return Json(new { result = true });
                    }
                    else
                    {
                        return Json(new { result = true, message = _localizer["You do not have permission to delete."].Value });
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
}