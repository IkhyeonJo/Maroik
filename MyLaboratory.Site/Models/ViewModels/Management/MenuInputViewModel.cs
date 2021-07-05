using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models.ViewModels.Management
{
    public class MenuInputViewModel
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string IconPath { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Role { get; set; }
        public int Order { get; set; }
    }
}