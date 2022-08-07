using MyLaboratory.Blog.Helpers;
using MyLaboratory.Blog.Models.ViewModels.Account;
using System.Threading.Tasks;

namespace MyLaboratory.Blog.Contracts
{
    public interface IMailRepository
    {
        /// <summary>
        /// ���� ��й�ȣ �ʱ�ȭ ������ HttpBody �κ��� ���մϴ�.
        /// </summary>
        /// <param name="loginInputViewModel"></param>
        /// <param name="title"></param>
        /// <param name="content0"></param>
        /// <param name="content1"></param>
        /// <returns></returns>
        public string GetMailResetPasswordBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1);
        /// <summary>
        /// ���� Ȯ�� ������ HttpBody �κ��� ���մϴ�.
        /// </summary>
        /// <param name="loginInputViewModel"></param>
        /// <param name="title"></param>
        /// <param name="content0"></param>
        /// <param name="content1"></param>
        /// <returns></returns>
        public string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1);
        /// <summary>
        /// ���� ����
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public Task<string> SendMailAsync(Mail mail);
    }
}