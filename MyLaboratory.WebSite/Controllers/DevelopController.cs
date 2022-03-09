using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyLaboratory.WebSite.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.WebSite.Services;
using Microsoft.AspNetCore.Authorization;
using MyLaboratory.WebSite.Common;
using MyLaboratory.WebSite.Contracts;
using MyLaboratory.WebSite.Helpers;
using System.Threading.Tasks;
using MyLaboratory.WebSite.Models.ViewModels.Develop;
using Microsoft.AspNetCore.Mvc.Localization;

namespace MyLaboratory.WebSite.Controllers
{
    public class DevelopController : Controller
    {
        private readonly IHtmlLocalizer<DevelopController> localizer;
        private readonly ILogger<DevelopController> logger;

        public DevelopController(IHtmlLocalizer<DevelopController> localizer, ILogger<DevelopController> logger)
        {
            this.localizer = localizer;
            this.logger = logger;
        }

        #region WebAPI

        #region Read
        [HttpGet]
        public IActionResult API()
        {
            return View();
        }
        #endregion

        #endregion

        #region MachineLearning

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public async Task<IActionResult> CreateMachineLearning(MachineLearningInputViewModel machineLearningInputViewModel)
        {
            if (machineLearningInputViewModel.MLModelDataFiles == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = localizer["Please attach a file"].Value });
            }
            else
            {
                var machineLearningModelDataFile = machineLearningInputViewModel.MLModelDataFiles[0];
                if (machineLearningModelDataFile.Length > 0 && machineLearningModelDataFile.Length <= 10485760) // machineLearningModelDataFile Length : 파일 크기 0MB 이상, 10MB 이하)
                {
                    if (machineLearningModelDataFile.ContentType.ToUpper() == "application/vnd.ms-excel".ToUpper())
                    {
                        //                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                        //                        string avatarPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", Regex.Replace($"{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}", string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), "_"));
                        //                        if (!Directory.Exists(avatarPath))
                        //                            Directory.CreateDirectory(avatarPath);
                        //                        string avatarFile = Regex.Replace($"{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}{Path.GetExtension(profileAvatarFile.FileName)}", string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), "_");
                        //                        string filePath = Path.Combine(avatarPath, avatarFile);

                        //#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        //                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
                        //#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        //                        {
                        //                            await profileAvatarFile.CopyToAsync(stream);
                        //                            await stream.FlushAsync();
                        //                        }

                        //                        try
                        //                        {
                        //                            var tempAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                        //                            tempAccount.AvatarImagePath = $"/upload/Management/Profile/{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}/{avatarFile}";
                        //                            await accountRepository.UpdateAccountAsync(tempAccount);
                        //                            return Ok(new { result = true });
                        //                        }
                        //                        catch (Exception e)
                        //                        {
                        //                            return Ok(new { result = false, errorMessage = e.ToString() });
                        //                        }
                        return Ok(new { result = true });
                    }
                    else // csv 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = localizer["Only .csv file allowed."].Value });
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

        #region Read
        [HttpGet]
        public IActionResult MachineLearning()
        {
            return View();
        }
        #endregion

        #endregion
    }
}