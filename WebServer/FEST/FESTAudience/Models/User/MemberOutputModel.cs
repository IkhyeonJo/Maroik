using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FESTRubricWebServer.Models.User
{
    public class MemberOutputModel
    {
        public int MemberID { get; set; }

        [Required(ErrorMessage = "학생 이름을 입력하세요. (Input studentName)")] // NOT NULL 설정ㅣ
        public string MemberName { get; set; }

        public bool IsForeigner { get; set; }

        //[Required(ErrorMessage = "주민번호을 입력하세요. (Input ResidentNumber)")] // NOT NULL 설정ㅣ
        public string ResidentNumber { get; set; }

        [Required(ErrorMessage = "학제를 선택하세요. (Input School System)")] // NOT NULL 설정ㅣ
        public string Level { get; set; }

        [Required(ErrorMessage = "학년을 입력하세요. (Input grade)")] // NOT NULL 설정ㅣ
        public int Grade { get; set; }

        [Required(ErrorMessage = "학교를 입력하세요. (Input School)")] // NOT NULL 설정ㅣ
        public string School { get; set; }

        public string MemberPhone { get; set; }
        public string MemberEmail { get; set; }

        [Required(ErrorMessage = "보호자 이름을 입력하세요. (Input Guardian Name (such as parent name))")] // NOT NULL 설정ㅣ
        public string GuardianName1 { get; set; }
        [Required(ErrorMessage = "보호자 핸드폰 번호을 입력하세요. (Input Guardian Phone number (such as parent phone number))")] // NOT NULL 설정ㅣ
        public string GuardianPhone1 { get; set; }
        [Required(ErrorMessage = "보호자 관계를 입력하세요. (Input Guardian's Relationship (such as mother, father))")] // NOT NULL 설정ㅣ
        public string RelationShip1 { get; set; }
    }
}
