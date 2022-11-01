using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Contracts;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext context;

        public CommentRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 한 게시물의 모든 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Comment>> GetOneBoardComments(int boardId)
        {
            return await context.Comments.Where(x => x.BoardId == boardId).ToListAsync();
        }

        /// <summary>
        /// 댓글을 생성합니다.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public async Task WriteCommentAsync(Comment comment)
        {
            await context.Comments.AddAsync(comment);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 댓글을 업데이트합니다.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public async Task UpdateCommentAsync(Comment comment)
        {
            context.Comments.Update(comment);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 계시물을 삭제합니다.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public async Task DeleteCommentAsync(Comment comment)
        {
            context.Comments.Update(comment);
            await context.SaveChangesAsync();
        }

    }
}