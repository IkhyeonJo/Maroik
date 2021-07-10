using MyLaboratory.Site.Models;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface IAccountRepository
    {
        public Task<Account> GetAccountAsync(string email);
        public Task CreateAccountAsync(Account account);
        public Task UpdateAccountAsync(Account account);
        public Task<LoginInputViewModel> SignUp(LoginInputViewModel loginInputViewModel);
        public Task<string> ConfirmMail(string guid);
    }
}