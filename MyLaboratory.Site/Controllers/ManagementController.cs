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
using MyLaboratory.Site.Helpers;
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

namespace MyLaboratory.Site.Controllers
{
    public class ManagementController : Controller
    {
        private readonly ILogger<ManagementController> logger;
        private readonly IProductRepository productRepository;
        private readonly IWorkerRepository workerRepository;
        private readonly IHostEnvironment hostEnvironment;
        private readonly ICategoryRepository categoryRepository;
        private readonly ISubCategoryRepository subCategoryRepository;
        public ManagementController(ILogger<ManagementController> logger, IProductRepository productRepository, IWorkerRepository workerRepository, IHostEnvironment hostEnvironment, ICategoryRepository categoryRepository, ISubCategoryRepository subCategoryRepository)
        {
            this.logger = logger;
            this.productRepository = productRepository;
            this.workerRepository = workerRepository;
            this.hostEnvironment = hostEnvironment;
            this.categoryRepository = categoryRepository;
            this.subCategoryRepository = subCategoryRepository;
        }

        [HttpGet]
        public IActionResult UserProfile()
        {
            return View();
        }

        #region Category
        [HttpGet]
        public IActionResult Category(string WholeSearch)
        {
            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest") // ajax
            {
                List<Product> products = new List<Product>();
                foreach (var item in productRepository.GetProducts)
                {
                    if (string.IsNullOrEmpty(WholeSearch))
                    {
                        products.Add(new Product() { Id = item.Id, Categoria = item.Categoria, Nome = item.Nome, Valor = item.Valor });
                    }
                    else
                    {
                        if (item.Id.ToString().Contains(WholeSearch) || item.Categoria.ToString().Contains(WholeSearch) || item.Nome.ToString().Contains(WholeSearch) || item.Valor.ToString().Contains(WholeSearch))
                        {
                            products.Add(new Product() { Id = item.Id, Categoria = item.Categoria, Nome = item.Nome, Valor = item.Valor });
                        }
                    }

                }
                var result = products.AsQueryable();
                return PartialView("_CategoryGrid", result);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CategoryExportExcel(string fileName = "")
        {

            // query data from database
            await Task.Yield();
            //var list = new List<Product>()
            //{
            //    new Product { Categoria= "catcher", Id= 18 },
            //    new Product { Categoria = "james", Id= 20 },
            //};

            var adminCategories = categoryRepository.AdminCategories;
            var adminSubCategories = subCategoryRepository.AdminSubCategories;
            var userCategories = categoryRepository.UserCategories;
            var userSubCategories = subCategoryRepository.UserSubCategories;

            var stream = new MemoryStream();

            using (var package = new ExcelPackage(stream))
            {
                var workSheetAdminCategories = package.Workbook.Worksheets.Add(nameof(adminCategories));
                workSheetAdminCategories.Cells.LoadFromCollection(adminCategories, true);

                var workSheetAdminSubCategories = package.Workbook.Worksheets.Add(nameof(adminSubCategories));
                workSheetAdminSubCategories.Cells.LoadFromCollection(adminSubCategories, true);

                var workSheetUserCategories = package.Workbook.Worksheets.Add(nameof(userCategories));
                workSheetUserCategories.Cells.LoadFromCollection(userCategories, true);

                var workSheetUserSubCategories = package.Workbook.Worksheets.Add(nameof(userSubCategories));
                workSheetUserSubCategories.Cells.LoadFromCollection(userSubCategories, true);
                // simple way
                //workSheet.Cells.LoadFromCollection(list, true);

                //// mutual
                //workSheet.Row(1).Height = 20;
                //workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                //workSheet.Row(1).Style.Font.Bold = true;
                //workSheet.Cells[1, 1].Value = "No";
                //workSheet.Cells[1, 2].Value = "Name";
                //workSheet.Cells[1, 3].Value = "Age";

                //int recordIndex = 2;
                //foreach (var item in list)
                //{
                //    workSheet.Cells[recordIndex, 1].Value = (recordIndex - 1).ToString();
                //    workSheet.Cells[recordIndex, 2].Value = item.UserName;
                //    workSheet.Cells[recordIndex, 3].Value = item.Age;
                //    recordIndex++;
                //}

                package.Save();
            }
            stream.Position = 0;
            string excelName = $"{fileName}-{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.xlsx";

            return File(stream, "application/octet-stream", excelName);
            //return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Category([FromHeader]string MyLaboratoryCustomActionMethod, [FromBody]Product product)
        //{
        //    if(MyLaboratoryCustomActionMethod == "ExportExcel") // CRUD 및 ExportExcel 등 모든 것을 하게 될 예정
        //    {
        //        try
        //        {
        //            string sWebRootFolder = hostEnvironment.ContentRootPath;
        //    string sFileName = @"Employees.xlsx";
        //    string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
        //    FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
        //    var memory = new MemoryStream();
        //    using (var fs = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Create, FileAccess.Write))
        //    {
        //        IWorkbook workbook;
        //        workbook = new XSSFWorkbook();
        //        ISheet excelSheet = workbook.CreateSheet("employee");
        //        IRow row = excelSheet.CreateRow(0);
        //        row.CreateCell(0).SetCellValue("EmployeeId");
        //        row.CreateCell(1).SetCellValue("EmployeeName");
        //        row.CreateCell(2).SetCellValue("Age");
        //        row.CreateCell(3).SetCellValue("Sex");
        //        row.CreateCell(4).SetCellValue("Designation");
        //        row = excelSheet.CreateRow(1);
        //        row.CreateCell(0).SetCellValue(1);
        //        row.CreateCell(1).SetCellValue("Jack Supreu");
        //        row.CreateCell(2).SetCellValue(45);
        //        row.CreateCell(3).SetCellValue("Male");
        //        row.CreateCell(4).SetCellValue("Solution Architect");
        //        row = excelSheet.CreateRow(2);
        //        row.CreateCell(0).SetCellValue(2);
        //        row.CreateCell(1).SetCellValue("Steve khan");
        //        row.CreateCell(2).SetCellValue(33);
        //        row.CreateCell(3).SetCellValue("Male");
        //        row.CreateCell(4).SetCellValue("Software Engineer");
        //        row = excelSheet.CreateRow(3);
        //        row.CreateCell(0).SetCellValue(3);
        //        row.CreateCell(1).SetCellValue("Romi gill");
        //        row.CreateCell(2).SetCellValue(25);
        //        row.CreateCell(3).SetCellValue("FeMale");
        //        row.CreateCell(4).SetCellValue("Junior Consultant");
        //        row = excelSheet.CreateRow(4);
        //        row.CreateCell(0).SetCellValue(4);
        //        row.CreateCell(1).SetCellValue("Hider Ali");
        //        row.CreateCell(2).SetCellValue(34);
        //        row.CreateCell(3).SetCellValue("Male");
        //        row.CreateCell(4).SetCellValue("Accountant");
        //        row = excelSheet.CreateRow(5);
        //        row.CreateCell(0).SetCellValue(5);
        //        row.CreateCell(1).SetCellValue("Mathew");
        //        row.CreateCell(2).SetCellValue(48);
        //        row.CreateCell(3).SetCellValue("Male");
        //        row.CreateCell(4).SetCellValue("Human Resource");
        //        workbook.Write(fs);
        //    }
        //    using (var stream = new FileStream(Path.Combine(sWebRootFolder, sFileName), FileMode.Open))
        //    {
        //        await stream.CopyToAsync(memory);
        //    }
        //    memory.Position = 0;
        //    return File(memory, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", sFileName);
        //        }
        //        catch (Exception e)
        //        {
        //            return Json(new { result = false, exception = e.ToString() });
        //        }
        //    }
        //    else // 이런 경우는 없음
        //    {
        //        return Json(new { result = false });
        //    }
        //}
        #endregion
    }
}