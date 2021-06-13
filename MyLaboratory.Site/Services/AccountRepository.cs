using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyLaboratory.Site.Data;
using MyLaboratory.Site.Models;
using MySqlConnector;

namespace MyLaboratory.Site.Services
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext context;

        public AccountRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public Account GetAccount(string email)
        {

            return context.Account.FirstOrDefault(x => x.Email == email);
        }

        public Account LockAccount(string email)
        {
            string sql = "Call LockAccount(@Email)";

            List<MySqlParameter> parms = new List<MySqlParameter>
            {
                // Create parameter(s)    
                new MySqlParameter { ParameterName = "@Email", Value = email}
            };

            context.Database.ExecuteSqlRaw(sql, parms.ToArray()); // call SP

            return null;
        }
    }
}