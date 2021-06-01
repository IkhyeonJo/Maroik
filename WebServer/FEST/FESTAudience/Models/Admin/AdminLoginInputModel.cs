using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class AdminLoginInputModel
    {
        [Required(ErrorMessage = "아이디를 입력하세요. (Input ID)")] // NOT NULL 설정
        public string ID { get; set; }

        [Required(ErrorMessage = "패스워드를 입력하세요. (Input Password)")] // NOT NULL 설정
        public string Password { get; set; }

        public string Category { get; set; }
        public int Level { get; set; }
    }
}
