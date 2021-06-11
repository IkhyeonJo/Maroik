using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyLaboratory.Site.Models.ViewModels
{
    [Table("Account")]
    public class AccountInputModel
    {

        [Key]
        [Required(ErrorMessage = "아이디를 입력하세요.")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string ID { get; set; }
        

        [Required(ErrorMessage = "패스워드를 입력하세요.")]
        [DataType(DataType.Password)]
        public string HashedPassword { get; set; }


        [Column(TypeName = "varchar(256)")]
        public string Role { get; set; }

    }
}
