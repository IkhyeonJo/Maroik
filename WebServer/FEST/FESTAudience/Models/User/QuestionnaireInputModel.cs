using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class QuestionnaireInputModel
    {
        public char ReadMe1 { get; set; }
        public char ReadMe2 { get; set; }

        [Required(ErrorMessage = "이름 입력하세요.")] // NOT NULL 설정
        public string Name { get; set; }

        [Required(ErrorMessage = "핸드폰 번호 입력하세요.")] // NOT NULL 설정
        public string PhoneNumber { get; set; }
        
        public string Email { get; set; }

        [Required(ErrorMessage = "소속 입력하세요.")] // NOT NULL 설정
        public string Affiliation { get; set; }

        [Required(ErrorMessage = "직책 입력하세요.")] // NOT NULL 설정
        public string Position { get; set; }

        public bool ChkFamily { get; set; }
        public bool ChkPrepareContest { get; set; }
        public bool ChkInfoRobot { get; set; }
        public bool ChkChildInfo { get; set; }
        public bool ChkFTCInfo { get; set; }
        public bool ChkCodingInfo { get; set; }
        public bool ChkRobotEduInfo { get; set; }
        public bool ChkEtc { get; set; }

        public string EtcContents { get; set; }
    }
}
