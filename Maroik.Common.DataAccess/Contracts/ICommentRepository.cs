using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface ICommentRepository
    {
        /// <summary>
        /// 한 게시물의 모든 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<Comment>> GetOneBoardComments(int boardId);

        /// <summary>
        /// 한 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<Comment> GetComment(int id);

        /// <summary>
        /// 댓글을 생성합니다.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Task WriteCommentAsync(Comment comment);

        /// <summary>
        /// 댓글을 삭제합니다.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public Task DeleteCommentAsync(Comment comment);

    }
}