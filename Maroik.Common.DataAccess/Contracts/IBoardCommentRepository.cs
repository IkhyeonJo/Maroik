using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface IBoardCommentRepository
    {
        /// <summary>
        /// 한 게시물의 모든 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<IEnumerable<BoardComment>> GetOneBoardComments(int boardId);

        /// <summary>
        /// 한 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<BoardComment> GetComment(int id);

        /// <summary>
        /// 댓글을 생성합니다.
        /// </summary>
        /// <param name="boardComment"></param>
        /// <returns></returns>
        public Task WriteCommentAsync(BoardComment boardComment);

        /// <summary>
        /// 댓글을 삭제합니다.
        /// </summary>
        /// <param name="boardComment"></param>
        /// <returns></returns>
        public Task DeleteCommentAsync(BoardComment boardComment);

    }
}