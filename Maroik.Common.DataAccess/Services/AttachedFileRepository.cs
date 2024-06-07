using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class AttachedFileRepository : IAttachedFileRepository
    {
        private readonly ApplicationDbContext _context;

        public AttachedFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 등록된 모든 계정을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<List<BoardAttachedFile>> GetAllAsync()
        {
            return await _context.BoardAttachedFiles.ToListAsync();
        }

        /// <summary>
        /// 첨부파일을 저장합니다.
        /// </summary>
        /// <param name="boardAttachedFile"></param>
        /// <returns></returns>
        public async Task SaveAttachedFileAsync(BoardAttachedFile boardAttachedFile)
        {
            _ = await _context.BoardAttachedFiles.AddAsync(boardAttachedFile);
            _ = await _context.SaveChangesAsync();
        }

        /// <summary>
        /// 첨부파일을 업데이트합니다.
        /// </summary>
        /// <param name="boardAttachedFile"></param>
        /// <returns></returns>
        public async Task UpdateAttachedFileAsync(BoardAttachedFile boardAttachedFile)
        {
            _ = _context.BoardAttachedFiles.Update(boardAttachedFile);
            _ = await _context.SaveChangesAsync();
        }
    }
}