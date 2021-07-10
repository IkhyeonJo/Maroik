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
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext context;

        public AccountRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        public async Task<Account> GetAccountAsync(string email)
        {
            return await context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
        }
        public async Task CreateAccountAsync(Account account)
        {
            await context.Accounts.AddAsync(account);
            await context.SaveChangesAsync();
        }
        public async Task UpdateAccountAsync(Account account)
        {
            context.Accounts.Update(account);
            await context.SaveChangesAsync();
        }

        public Task<string> ConfirmMail(string guid)
        {
            throw new System.NotImplementedException();
        }

        public Task<LoginInputViewModel> SignUp(LoginInputViewModel loginInputViewModel)
        {
            throw new System.NotImplementedException();
        }
    }
}