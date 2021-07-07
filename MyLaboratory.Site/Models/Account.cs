using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models
{
    [Table("Account")]
    public partial class Account
    {
        [Key]
        [Required(ErrorMessage = "아이디를 입력하세요.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "패스워드를 입력하세요.")]
        [DataType(DataType.Password)]
        public string HashedPassword { get; set; }
        [StringLength(255)]
        [Required(AllowEmptyStrings = false)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string FullName { get; set; }
        [StringLength(255)]
        [Required(AllowEmptyStrings = false)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Role { get; set; }
        public bool Locked { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool AgreedServiceTerms { get; set; }
        public string Guid { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string Message { get; set; }
    }
}
