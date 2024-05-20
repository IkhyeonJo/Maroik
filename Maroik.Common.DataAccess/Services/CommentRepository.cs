using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 한 게시물의 모든 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Comment>> GetOneBoardComments(int boardId)
        {
            return await _context.Comments.Where(x => x.BoardId == boardId).OrderBy(m => m.Order).ToListAsync();
        }

        /// <summary>
        /// 한 댓글을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<Comment> GetComment(int id)
        {
            return await _context.Comments.Where(x => x.Id == id).FirstOrDefaultAsync();
        }

        /// <summary>
        /// 댓글을 생성합니다.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public async Task WriteCommentAsync(Comment comment)
        {
            _ = await _context.Comments.AddAsync(comment);
            _ = await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 계시물을 삭제합니다.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public async Task DeleteCommentAsync(Comment comment)
        {
            _ = _context.Comments.Update(comment);
            _ = await _context.SaveChangesAsync();
        }

    }
}