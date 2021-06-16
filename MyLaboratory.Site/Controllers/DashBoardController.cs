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

namespace MyLaboratory.Site.Controllers
{
    [Authorize(Roles = Role.Admin)]
    public class DashBoardController : Controller
    {
        private readonly ILogger<DashBoardController> logger;
        private readonly IProductRepository productRepository;
        private readonly IWorkerRepository workerRepository;

        public DashBoardController(ILogger<DashBoardController> logger, IProductRepository productRepository, IWorkerRepository workerRepository)
        {
            this.logger = logger;
            this.productRepository = productRepository;
            this.workerRepository = workerRepository;
        }

        public IActionResult Index()
        {
            return View(productRepository.GetProducts);
        }

        [Authorize(Roles = Role.Admin)]
        public IActionResult Karl()
        {
            return View(workerRepository.GetWorkers);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
