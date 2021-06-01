using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class LoginInputModel
    {
        public string Category { get; set; }
        public string Year { get; set; }
        public string PersonalInformation { get; set; }

        public string CoachName { get; }
        /// <summary>
        /// 팀 번호
        /// </summary>
        [Required(ErrorMessage = "팀 번호를 입력하세요. (Input Team Number)")] // NOT NULL 설정
        public int TeamNo { get; set; }

        /// <summary>
        /// 사용자 이메일
        /// </summary>
        [Key] //PK 설정
        [Required(ErrorMessage = "이메일 주소를 입력해주세요. (Input Email Address)")]
        public string CoachEmail { get; set; }

        /// <summary>
        /// 사용자 비밀번호
        /// </summary>
        [Required(ErrorMessage = "패스워드를 입력하세요. (Input Password)")] // NOT NULL 설정
        public string CoachPassword { get; set; }
    }
}
