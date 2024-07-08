using Maroik.Common.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts
{
    public interface ICalendarRepository
    {
        /// <summary>
        /// 로그인 계정에 해당하는 모든 달력을 구합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public Task<List<Calendar>> GetCalendarsAsync(string email);

        /// <summary>
        /// 달력을 생성합니다.
        /// </summary>
        /// <param name="calendar"></param>
        /// <returns></returns>
        public Task CreateCalendarAsync(Calendar calendar);
    }
}