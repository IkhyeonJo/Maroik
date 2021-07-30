using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models.ViewModels.Management
{
    public class ProfileOutputViewModel
    {
        [Required(ErrorMessage = "Please enter Email")]
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Please enter AvatarImagePath")]
        [Display(Name = "AvatarImagePath")]
        public string AvatarImagePath { get; set; }
        [Required(ErrorMessage = "Please enter Full name")]
        [Display(Name = "FullName")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Please enter Created")]
        [Display(Name = "Created")]
        public DateTime Created { get; set; }
    }
}