using MyLaboratory.Site.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Services
{
    public interface IAccountRepository
    {
        public Account GetAccount(string email);
    }
}