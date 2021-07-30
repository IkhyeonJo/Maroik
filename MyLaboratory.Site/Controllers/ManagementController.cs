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

namespace MyLaboratory.Site.Controllers
{
    public class ManagementController : Controller
    {
        private readonly ILogger<ManagementController> logger;
        private readonly IAccountRepository accountRepository;
        private readonly IProductRepository productRepository;
        private readonly IWorkerRepository workerRepository;
        private readonly IHostEnvironment hostEnvironment;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        public ManagementController(ILogger<ManagementController> logger, IAccountRepository accountRepository, IProductRepository productRepository, IWorkerRepository workerRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
        {
            this.logger = logger;
            this.accountRepository = accountRepository;
            this.productRepository = productRepository;
            this.workerRepository = workerRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
        }

        #region Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
            var tempAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);

            return View(new ProfileOutputViewModel() { Email = tempAccount.Email, AvatarImagePath = tempAccount.AvatarImagePath, FullName = tempAccount.FullName, Created = tempAccount.Created });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        [RequiredHttpPostAccess(Role = Role.User)]
        public async Task<IActionResult> UpdateProfileAvatar(ProfileInputViewModel profileInputViewModel)
        {
            if (profileInputViewModel.ProfileAvatarFiles == null) // 파일 없을 시
            {
                return Ok(new { result = false, errorMessage = "파일 첨부하여 주십시오. (Please attach a file)" });
            }
            else
            {
                var profileAvatarFile = profileInputViewModel.ProfileAvatarFiles[0];
                if (profileAvatarFile.Length > 0 && profileAvatarFile.Length <= 10485760) // profileAvatarFile Length : 파일 크기 0MB 이상, 10MB 이하)
                {
                    if (profileAvatarFile.ContentType.ToUpper() == "image/png".ToUpper() || profileAvatarFile.ContentType.ToUpper() == "image/jpeg".ToUpper())
                    {
                        HttpContext.Session.TryGetValue("AccountSession", out byte[] resultByte);
                        string avatarPath = Path.Combine(hostEnvironment.ContentRootPath, "wwwroot", "upload", "Management", "Profile", Regex.Replace($"{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}", string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), "_"));
                        if (!Directory.Exists(avatarPath))
                            Directory.CreateDirectory(avatarPath);
                        string avatarFile = Regex.Replace($"{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}{Path.GetExtension(profileAvatarFile.FileName)}", string.Format(@"([{0}]*\.+$)|([{0}]+)", Regex.Escape(new string(Path.GetInvalidFileNameChars()))), "_");
                        string filePath = Path.Combine(avatarPath, avatarFile);

#pragma warning disable SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        using (FileStream stream = new FileStream(filePath, FileMode.Create))
#pragma warning restore SCS0018 // Potential Path Traversal vulnerability was found where '{0}' in '{1}' may be tainted by user-controlled data from '{2}' in method '{3}'.
                        {
                            await profileAvatarFile.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }

                        try
                        {
                            var tempAccount = await accountRepository.GetAccountAsync(JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email);
                            tempAccount.AvatarImagePath = $"/upload/Management/Profile/{JsonConvert.DeserializeObject<Account>(Encoding.Default.GetString(resultByte)).Email}/{avatarFile}";
                            await accountRepository.UpdateAccountAsync(tempAccount);
                            return Ok(new { result = true });
                        } 
                        catch (Exception e)
                        {
                            return Ok(new { result = false, errorMessage = e.ToString() });
                        }
                    }
                    else // 이미지 png 또는 jpg 외 확장자는 거절
                    {
                        return Ok(new { result = false, errorMessage = "Only .jpg or .png file allowed" });
                    }
                }
                else // fileSizeLimitationErrorMessage
                {
                    return Ok(new { result = false, errorMessage = "파일 크기는 10MB 이하만 가능합니다. (File Size must be smaller than 10MB.)" });
                }
                // process uploaded files 
                // Don't rely on or trust the FileName property without validation.
            }
        }

        #endregion

        #region Menu

        #region Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult CreateCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
                    Category category = new Category
                    {
                        Name = menuInputViewModel.Name,
                        DisplayName = menuInputViewModel.DisplayName,
                        IconPath = menuInputViewModel.IconPath,
                        Controller = menuInputViewModel.Controller,
                        Action = menuInputViewModel.Action,
                        Role = menuInputViewModel.Role,
                        Order = menuInputViewModel.Order
                    };
                    categoryRepository.CreateCategory(category);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult CreateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
                    SubCategory subCategory = new SubCategory
                    {
                        CategoryId = menuInputViewModel.CategoryId,
                        Name = menuInputViewModel.Name,
                        DisplayName = menuInputViewModel.DisplayName,
                        IconPath = menuInputViewModel.IconPath,
                        Action = menuInputViewModel.Action,
                        Role = menuInputViewModel.Role,
                        Order = menuInputViewModel.Order
                    };
                    subCategoryRepository.CreateSubCategory(subCategory);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Read
        [HttpGet]
        public IActionResult Menu(string wholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<MenuOutputViewModel> menuOutputViewModels = new List<MenuOutputViewModel>();

                #region AdminCategories
                foreach (var item in categoryRepository.AdminCategories)
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
                foreach (var item in categoryRepository.UserCategories)
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
                foreach (var item in subCategoryRepository.AdminSubCategories)
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
                foreach (var item in subCategoryRepository.UserSubCategories)
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


                var result = menuOutputViewModels.AsQueryable();
                return PartialView("_MenuGrid", result);
            }

            return View(new MenuOutputViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult IsCategoryExists(int id)
        {
            try
            {
                List<MenuOutputViewModel> menuOutputViewModels = new List<MenuOutputViewModel>();

                #region AdminCategories
                foreach (var item in categoryRepository.AdminCategories)
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
                foreach (var item in categoryRepository.UserCategories)
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
                var selectedCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault();
                if (selectedCategory == null)
                {
                    throw new Exception(); // DB에서 못 찾음
                }
                return Json(new { result = true, category = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult IsSubCategoryExists(int id)
        {
            try
            {
                List<MenuOutputViewModel> menuOutputViewModels = new List<MenuOutputViewModel>();

                #region AdminCategories
                foreach (var item in subCategoryRepository.AdminSubCategories)
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

                #region UserCategories
                foreach (var item in subCategoryRepository.UserSubCategories)
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
                var selectedCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault();
                if (selectedCategory == null)
                {
                    throw new Exception(); // DB에서 못 찾음
                }
                return Json(new { result = true, subCategory = menuOutputViewModels.Where(a => a.Id == id).FirstOrDefault() });
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult UpdateCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
                    Category category = new Category
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
                    categoryRepository.UpdateCategory(category);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult UpdateSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
                    SubCategory subCategory = new SubCategory
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
                    subCategoryRepository.UpdateSubCategory(subCategory);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        #endregion

        #region Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult DeleteCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Action)); // remove ModelState check in Action (in Request parameters, Action can be null)

                if (ModelState.IsValid)
                {
                    Category category = new Category
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
                    categoryRepository.DeleteCategory(category);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequiredHttpPostAccess(Role = Role.Admin)]
        public IActionResult DeleteSubCategory([FromBody] MenuInputViewModel menuInputViewModel)
        {
            try
            {
                ModelState.Remove(nameof(menuInputViewModel.Controller)); // remove ModelState check in Controller (in Request parameters, Controller can be null)

                if (ModelState.IsValid)
                {
                    SubCategory subCategory = new SubCategory
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
                    subCategoryRepository.DeleteSubCategory(subCategory);

                    return Json(new { result = true });
                }
                else
                {
                    return Json(new { result = false, error = "Input is invalid" });
                }
            }
            catch (Exception e)
            {
                return Json(new { result = false, error = e.ToString() });
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

            List<MenuOutputViewModel> menuOutputViewModels = new List<MenuOutputViewModel>();

            #region AdminCategories
            foreach (var item in categoryRepository.AdminCategories)
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
            foreach (var item in categoryRepository.UserCategories)
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
            foreach (var item in subCategoryRepository.AdminSubCategories)
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
            foreach (var item in subCategoryRepository.UserSubCategories)
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

            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                var workSheetMenuOutputViewModels = package.Workbook.Worksheets.Add("Sheet1");
                workSheetMenuOutputViewModels.Cells.LoadFromCollection(menuOutputViewModels, true);

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{DateTime.UtcNow.ToString("yyyyMMddHHmmssfff")}.xlsx";

            return File(stream, "application/octet-stream", excelName);
        }
        #endregion

        #endregion
    }
}