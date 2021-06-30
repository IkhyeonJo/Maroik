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

namespace MyLaboratory.Site.Controllers
{
    public class ManagementController : Controller
    {
        private readonly ILogger<ManagementController> logger;
        private readonly IProductRepository productRepository;
        private readonly IWorkerRepository workerRepository;

        public ManagementController(ILogger<ManagementController> logger, IProductRepository productRepository, IWorkerRepository workerRepository)
        {
            this.logger = logger;
            this.productRepository = productRepository;
            this.workerRepository = workerRepository;
        }

        public IActionResult UserProfile()
        {
            return View();
        }
        
        public IActionResult Category(string WholeSearch)
        {
            //List<Product> aa = new List<Product>();
            //foreach(var item in productRepository.GetProducts)
            //{
            //    aa.Add(new Product() { Id = item.Id, Categoria=item.Categoria, Nome=item.Nome, Valor=item.Valor });
            //}
            //var result = aa.AsQueryable();
            //return View(result);

            if (HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                List<Product> aa = new List<Product>();
                foreach (var item in productRepository.GetProducts)
                {
                    if (string.IsNullOrEmpty(WholeSearch))
                    {
                        aa.Add(new Product() { Id = item.Id, Categoria = item.Categoria, Nome = item.Nome, Valor = item.Valor });
                    }
                    else
                    {
                        if(item.Id.ToString().Contains(WholeSearch) || item.Categoria.ToString().Contains(WholeSearch) || item.Nome.ToString().Contains(WholeSearch) || item.Valor.ToString().Contains(WholeSearch))
                        {
                            aa.Add(new Product() { Id = item.Id, Categoria = item.Categoria, Nome = item.Nome, Valor = item.Valor });
                        }
                    }
                    
                }
                var result = aa.AsQueryable();
                return PartialView("_CategoryGrid", result);
            }
                
            return View();
        }
    }
}