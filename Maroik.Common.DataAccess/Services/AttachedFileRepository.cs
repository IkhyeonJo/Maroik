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
        private readonly ApplicationDbContext context;

        public AttachedFileRepository(ApplicationDbContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// 등록된 모든 계정을 구합니다.
        /// </summary>
        /// <returns></returns>
        public async Task<List<AttachedFile>> GetAllAsync()
        {
            return await context.AttachedFiles.ToListAsync();
        }

        /// <summary>
        /// 첨부파일을 저장합니다.
        /// </summary>
        /// <param name="attachedFile"></param>
        /// <returns></returns>
        public async Task SaveAttachedFileAsync(AttachedFile attachedFile)
        {
            _ = await context.AttachedFiles.AddAsync(attachedFile);
            _ = await context.SaveChangesAsync();
        }

        /// <summary>
        /// 첨부파일을 업데이트합니다.
        /// </summary>
        /// <param name="attachedFile"></param>
        /// <returns></returns>
        public async Task UpdateAttachedFileAsync(AttachedFile attachedFile)
        {
            _ = context.AttachedFiles.Update(attachedFile);
            _ = await context.SaveChangesAsync();
        }
    }
}