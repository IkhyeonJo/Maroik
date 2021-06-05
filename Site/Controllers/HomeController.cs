using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Site.Models;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Site.Models.Home;
using System;
using Microsoft.EntityFrameworkCore;

namespace Site.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> logger;
        private readonly IRepository repository;
        private string message;

        public HomeController(ILogger<HomeController> logger, IRepository repository, IConfiguration config)
        {
            this.logger = logger;
            this.repository = repository;
            message = $"Docker - ({config["HOSTNAME"]})";
        }

        public IActionResult Index()
        {

            //List<Product> products = new List<Product>();

            //using (MySqlConnection con = new MySqlConnection("server=localhost;port=3306;userid=root;password=root;database=ikhyeondb;"))
            ////using (MySqlConnection con = new MySqlConnection("server=mysql;port=3306;userid=root;password=root;database=ikhyeondb;"))
            //{
            //    con.Open();
            //    MySqlCommand cmd = new MySqlCommand("SELECT * FROM Product", con);
            //    MySqlDataReader reader = cmd.ExecuteReader();

            //    while (reader.Read())
            //    {
            //        //Extract your Data.
            //        Product product = new Product
            //        {
            //            Id = Convert.ToInt32(reader["id"]),
            //            Nome = reader["Nome"].ToString(),
            //            Valor = Convert.ToDecimal(reader["Valor"]),
            //            Categoria = reader["Categoria"].ToString()
            //        };

            //        products.Add(product);
            //    }
            //    reader.Close();
            //    con.Close();
            //}

            //var a = new AppDbContext();
            //var kk = a.Product.FromSqlRaw("SELECT * FROM dbo.Blogs");

            var aaa = repository.GetProducts;

            return View(aaa);
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
