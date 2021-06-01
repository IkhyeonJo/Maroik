using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class ResetPasswordInputModel
    {
        public string CoachName { get; }
        public string CoachPassword { get; set; }
        [Required(ErrorMessage = "팀 번호를 입력하세요. (Input Team Number)")] // NOT NULL 설정
        public int TeamNo { get; set; }

        [Required(ErrorMessage = "이메일 주소를 입력해주세요. (Input Email Address)")] // NOT NULL 설정
        public string CoachEmail { get; set; }
    }
}
