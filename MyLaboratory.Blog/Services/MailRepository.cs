using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MyLaboratory.Blog.Common;
using MyLaboratory.Blog.Contracts;
using MyLaboratory.Blog.Helpers;
using MyLaboratory.Blog.Models.ViewModels.Account;

namespace MyLaboratory.Blog.Services
{
    public class MailRepository : IMailRepository
    {
        /// <summary>
        /// ���� ��й�ȣ �ʱ�ȭ ������ HttpBody �κ��� ���մϴ�.
        /// </summary>
        /// <param name="loginInputViewModel"></param>
        /// <param name="title"></param>
        /// <param name="content0"></param>
        /// <param name="content1"></param>
        /// <returns></returns>
        public string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1)
        {
            string url = $"{ServerSetting.DomainName}Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(loginInputViewModel.RegistrationToken)}";
            return @$"<div style='text-align:centre;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
        }
        /// <summary>
        /// ���� Ȯ�� ������ HttpBody �κ��� ���մϴ�.
        /// </summary>
        /// <param name="loginInputViewModel"></param>
        /// <param name="title"></param>
        /// <param name="content0"></param>
        /// <param name="content1"></param>
        /// <returns></returns>
        public string GetMailResetPasswordBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1)
        {
            string url = $"{ServerSetting.DomainName}Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(loginInputViewModel.ResetPasswordToken)}";
            return @$"<div style='text-align:centre;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
        }
        /// <summary>
        /// ���� ����
        /// </summary>
        /// <param name="mail"></param>
        /// <returns></returns>
        public async Task<string> SendMailAsync(Mail mail)
        {
            try
            {
                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(mail.FromEmail, mail.FromFullName);
                    mail.ToMailIds.ForEach(x => {
                        mailMessage.To.Add(x);
                    });
                    mailMessage.Subject = mail.Subject;
                    mailMessage.Body = mail.Body;
                    mailMessage.IsBodyHtml = mail.IsBodyHTML;
                    mail.Attachments.ForEach(x => {
                        mailMessage.Attachments.Add(new Attachment(x));
                    });

                    using (SmtpClient smtp = new SmtpClient(mail.SmtpHost, mail.SmtpPort))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(mail.SmtpUserName, mail.SmtpPassword);
                        smtp.EnableSsl = mail.SmtpSSL;
                        await smtp.SendMailAsync(mailMessage);
                        return AccountMessage.MailSent;
                    }
                }
            }
            catch (Exception ex)
            {

                return ex.Message;
            }
        }
    }
}