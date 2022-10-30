using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Contracts;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class BoardRepository : IBoardRepository
    {
        private readonly ApplicationDbContext context;

        public BoardRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 한 종류의 모든 게시물을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Board>> GetOneTypeBoards(string type)
        {
            return await context.Boards.Where(x => x.Type == type).ToListAsync();
        }

        /// <summary>
        /// 게시물을 생성합니다.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public async Task WriteBoardAsync(Board board)
        {
            await context.Boards.AddAsync(board);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 계시물을 업데이트합니다.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public async Task UpdateBoardAsync(Board board)
        {
            context.Boards.Update(board);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 계시물을 삭제합니다.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public async Task DeleteBoardAsync(Board board)
        {
            context.Boards.Update(board);
            await context.SaveChangesAsync();
        }

    }
}