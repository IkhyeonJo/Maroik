using MyLaboratory.Site.Models;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface IIncomeRepository
    {
        /// <summary>
        /// 로그인 계정에 해당하는 모든 수입을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<List<Income>> GetIncomesAsync(string email);

        /// <summary>
        /// 수입을 생성합니다.
        /// </summary>
        /// <param name="income"></param>
        /// <returns></returns>
        public Task CreateIncomeAsync(Income income);
    }
}