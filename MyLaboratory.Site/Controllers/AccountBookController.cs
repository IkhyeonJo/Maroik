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
    public class AccountBookController : Controller
    {
        private readonly ILogger<AccountBookController> logger;
        private readonly IProductRepository productRepository;
        private readonly IWorkerRepository workerRepository;

        public AccountBookController(ILogger<AccountBookController> logger, IProductRepository productRepository, IWorkerRepository workerRepository)
        {
            this.logger = logger;
            this.productRepository = productRepository;
            this.workerRepository = workerRepository;
        }

        public IActionResult Main()
        {
            return View();
        }
        
        public IActionResult Chart()
        {
            return View();
        }
    }
}
