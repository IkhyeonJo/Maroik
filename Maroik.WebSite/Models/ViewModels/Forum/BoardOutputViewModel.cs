using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.Forum
{
    public class BoardOutputViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Must be between 1 and 255 characters", MinimumLength = 1)]
        public string Title { get; set; }
        
        public string Content { get; set; }

        public string AttachedFilePath { get; set; }
    }
}
