using MyLaboratory.Site.Models;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyLaboratory.Common.DataAccess.Models;

namespace MyLaboratory.Site.Contracts
{
    public interface IExpenditureRepository
    {
        /// <summary>
        /// 로그인 계정에 해당하는 모든 지출을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<List<Expenditure>> GetExpendituresAsync(string email);

        /// <summary>
        /// 지출을 생성합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public Task CreateExpenditureAsync(Expenditure expenditure);

        /// <summary>
        /// 지출을 업데이트합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public Task UpdateExpenditureAsync(Expenditure expenditure);

        /// <summary>
        /// 지출을 제거합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public Task DeleteExpenditureAsync(Expenditure expenditure);
    }
}