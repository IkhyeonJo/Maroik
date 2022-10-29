using System.Collections.Generic;
using System.Threading.Tasks;
using Maroik.Common.DataAccess.Models;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface IAttachedFileRepository
    {
        /// <summary>
        /// 등록된 모든 파일을 구합니다.
        /// </summary>
        /// <returns></returns>
        public Task<List<AttachedFile>> GetAllAsync();
    }
}