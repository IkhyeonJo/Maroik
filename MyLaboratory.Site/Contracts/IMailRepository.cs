using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models.ViewModels.Account;
using System.Threading.Tasks;

namespace MyLaboratory.Site.Contracts
{
    public interface IMailRepository
    {
        string GetMailResetPasswordBody(LoginInputViewModel loginInputViewModel);
        string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1);
        Task<string> SendMail(Mail mail);
    }
}