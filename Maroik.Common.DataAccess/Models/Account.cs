using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Models
{
    /// <summary>
    /// Maroik.WebSite 계정
    /// </summary>
    public partial class Account
    {
        public Account()
        {
            Assets = new HashSet<Asset>();
        }

        /// <summary>
        /// 이메일 (ID)
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 해시화 된 비밀번호
        /// </summary>
        public string HashedPassword { get; set; }
        /// <summary>
        /// 닉네임
        /// </summary>
        public string Nickname { get; set; }
        /// <summary>
        /// 아바타 이미지 경로
        /// </summary>
        public string AvatarImagePath { get; set; }
        /// <summary>
        /// 역할 (Admin 또는 User)
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// 잠금
        /// </summary>
        public bool Locked { get; set; }
        /// <summary>
        /// 로그인 시도 횟수
        /// </summary>
        public int LoginAttempt { get; set; }
        /// <summary>
        /// 이메일 확인 여부
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// 약관 동의 여부
        /// </summary>
        public bool AgreedServiceTerms { get; set; }
        /// <summary>
        /// 회원가입 인증 토큰
        /// </summary>
        public string RegistrationToken { get; set; }
        /// <summary>
        /// 비밀번호 찾기 인증 토큰
        /// </summary>
        public string ResetPasswordToken { get; set; }
        /// <summary>
        /// 생성일
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// 업데이트일
        /// </summary>
        public DateTime Updated { get; set; }
        /// <summary>
        /// 상태 메시지
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 삭제 여부
        /// </summary>
        public bool Deleted { get; set; }

        public virtual ICollection<Asset> Assets { get; set; }
    }
}
