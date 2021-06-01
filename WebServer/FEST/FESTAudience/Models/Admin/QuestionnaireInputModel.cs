using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.Admin
{
    public class QuestionnaireInputModel
    {
        public int PK { get; set; }
        
        [Required(ErrorMessage = "이름 입력하세요.")] // NOT NULL 설정
        public string Name { get; set; }

        [Required(ErrorMessage = "핸드폰 번호 입력하세요.")] // NOT NULL 설정
        public string PhoneNumber { get; set; }
        
        public string Email { get; set; }

        [Required(ErrorMessage = "소속 입력하세요.")] // NOT NULL 설정
        public string Affiliation { get; set; }

        [Required(ErrorMessage = "직책 입력하세요.")] // NOT NULL 설정
        public string Position { get; set; }

        public bool Checked { get; set; }

        public string State { get; set; }
    }
}
