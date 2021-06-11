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
    public class Account
    {
        [Key]
        [Required(ErrorMessage = "아이디를 입력하세요.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string Email { get; set; }
        

        [Required(ErrorMessage = "패스워드를 입력하세요.")]
        [DataType(DataType.Password)]
        public string HashedPassword { get; set; }


        [Column(TypeName = "varchar(256)")]
        public string Role { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string FirstName { get; set; }

        [Column(TypeName = "varchar(256)")]
        public string LastName { get; set; }

        public bool IsLocked { get; set; }
    }
}
