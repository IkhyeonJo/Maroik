using MyLaboratory.Site.Models;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface IAssetRepository
    {
        /// <summary>
        /// 로그인 계정에 해당하는 모든 자산을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<List<Asset>> GetAssetsAsync(string email);
    }
}