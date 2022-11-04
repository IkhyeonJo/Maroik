using System;
using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models
{
    /// <summary>
    /// Maroik.WebSite 게시판
    /// </summary>
    public partial class Board
    {
        public Board()
        {
            AttachedFiles = new HashSet<AttachedFile>();
            Comments = new HashSet<Comment>();
        }

        /// <summary>
        /// PK
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 형태 (자유)
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// 제목
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 내용
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 작성자
        /// </summary>
        public string Writer { get; set; }
        /// <summary>
        /// 생성일
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// 업데이트일
        /// </summary>
        public DateTime Updated { get; set; }
        /// <summary>
        /// 조회수
        /// </summary>
        public int Views { get; set; }
        /// <summary>
        /// 삭제 여부
        /// </summary>
        public bool Deleted { get; set; }
        /// <summary>
        /// 잠금 여부
        /// </summary>
        public bool Locked { get; set; }

        public virtual ICollection<AttachedFile> AttachedFiles { get; set; }
        public virtual ICollection<Comment> Comments { get; set; }
    }
}
