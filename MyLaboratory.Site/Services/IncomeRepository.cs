using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Models;
using MyLaboratory.Site.Contracts;
using MyLaboratory.Site.Models.ViewModels.Account;

namespace MyLaboratory.Site.Services
{
    public class IncomeRepository : IIncomeRepository
    {
        private readonly ApplicationDbContext context;

        public IncomeRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 로그인 계정에 해당하는 모든 수입을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<List<Income>> GetIncomesAsync(string email)
        {
            return await context.Incomes.Where(x => x.AccountEmail == email).ToListAsync();
        }

        /// <summary>
        /// 수입을 생성합니다.
        /// </summary>
        /// <param name="income"></param>
        /// <returns></returns>
        public async Task CreateIncomeAsync(Income income)
        {
            await context.Incomes.AddAsync(income);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// 수입을 업데이트합니다.
        /// </summary>
        /// <param name="income"></param>
        /// <returns></returns>
        public async Task UpdateIncomeAsync(Income income)
        {
            context.Incomes.Update(income);
            await context.SaveChangesAsync();
        }
    }
}