using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface IMailRepository
    {
        string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel);
        Task<string> SendMail(Mail mail);
    }
}