using System;
using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models
{
    /// <summary>
    /// Maroik.WebSite 첨부 파일
    /// </summary>
    public partial class AttachedFile
    {
        /// <summary>
        /// PK
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// 부모 게시판 Id
        /// </summary>
        public int ForumId { get; set; }
        /// <summary>
        /// 순번
        /// </summary>
        public int Order { get; set; }
        /// <summary>
        /// 이름
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 확장자
        /// </summary>
        public string Extension { get; set; }
        /// <summary>
        /// 부모 댓글 번호
        /// </summary>
        public string Path { get; set; }

        public virtual Forum Forum { get; set; }
    }
}
