using System.ComponentModel.DataAnnotations;

namespace Maroik.WebSite.Models.ViewModels.Forum
{
    public class BoardOutputViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Must be between 1 and 100 characters.", MinimumLength = 1)]
        public string Title { get; set; }

        public string Content { get; set; }

        public bool Locked { get; set; }

        public string Writer { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public int Views { get; set; }

        public string BoardAttachedFileName { get; set; }

        public string BoardAttachedFileExtension { get; set; }

        public int BoardAttachedFileSize { get; set; }

        public string BoardAttachedFilePath { get; set; }
    }
}
