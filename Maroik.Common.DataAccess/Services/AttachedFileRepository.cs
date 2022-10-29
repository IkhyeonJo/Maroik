using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Maroik.Common.DataAccess.Contracts;

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
    }
}