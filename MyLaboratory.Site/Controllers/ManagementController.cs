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
        
        public IActionResult Category()
        {
            return View();
        }
    }
}
