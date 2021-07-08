using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models.ViewModels.Account
{
    public class LoginInputViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
