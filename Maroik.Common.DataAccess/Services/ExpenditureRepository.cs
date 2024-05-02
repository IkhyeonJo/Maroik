using Maroik.Common.DataAccess.Contracts;
using Maroik.Common.DataAccess.Data;
using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Services
{
    public class ExpenditureRepository : IExpenditureRepository
    {
        private readonly ApplicationDbContext context;

        public ExpenditureRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// 로그인 계정에 해당하는 모든 지출을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<List<Expenditure>> GetExpendituresAsync(string email)
        {
            return await context.Expenditures.Where(x => x.AccountEmail == email).ToListAsync();
        }

        /// <summary>
        /// 로그인 계정에 해당하는 금년월 지출을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="yearMonth"></param>
        /// <returns></returns>
        public async Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId)
        {
            return await context.Expenditures.FromSqlInterpolated
                    (
                        @$"SELECT *
                            FROM Expenditure
                            WHERE 
                            DATE_FORMAT(CONVERT_TZ(Created, 'UTC', {timeZoneIanaId}), '%Y-%m') = {yearMonth}
                            AND AccountEmail = {email}"
                    ).ToListAsync();
        }

        /// <summary>
        /// 지출을 생성합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public async Task CreateExpenditureAsync(Expenditure expenditure)
        {
            _ = await context.Expenditures.AddAsync(expenditure);
            _ = await context.SaveChangesAsync();
        }

        /// <summary>
        /// 지출을 업데이트합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public async Task UpdateExpenditureAsync(Expenditure expenditure)
        {
            _ = context.Expenditures.Update(expenditure);
            _ = await context.SaveChangesAsync();
        }

        /// <summary>
        /// 지출을 제거합니다.
        /// </summary>
        /// <param name="expenditure"></param>
        /// <returns></returns>
        public async Task DeleteExpenditureAsync(Expenditure expenditure)
        {
            _ = context.Expenditures.Remove(expenditure);
            _ = await context.SaveChangesAsync();
        }

        /// <summary>
        /// 로그인 계정에 해당하는 금년월 지출 통계를 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="yearMonth"></param>
        /// <returns></returns>
        public async Task<List<USP_Expenditure_MonthlyReport>> GetCurrentYearMonthExpendituresMonthlyReport(string email, string yearMonth, string timeZoneIanaId)
        {
            return await context.USP_Expenditure_MonthlyReports.FromSqlRaw("CALL USP_Expenditure_MonthlyReport(@yearMonth, @email, @timeZoneIanaId)", new MySqlParameter("@yearMonth", yearMonth), new MySqlParameter("@email", email), new MySqlParameter("@timeZoneIanaId", timeZoneIanaId)).ToListAsync();
        }
    }
}