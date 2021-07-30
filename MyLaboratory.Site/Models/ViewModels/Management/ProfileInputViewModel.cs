using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models.ViewModels.Management
{
    public class ProfileInputViewModel
    {
        [Required(ErrorMessage = "Please enter Full name")]
        [Display(Name = "FullName")]
        public string FullName { get; set; }
        public List<IFormFile> ProfileAvatarFiles { get; set; }
    }
}