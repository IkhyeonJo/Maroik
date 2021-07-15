using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using MyLaboratory.Site.Common;
using MyLaboratory.Site.Contracts;
using MyLaboratory.Site.Helpers;
using MyLaboratory.Site.Models.ViewModels.Account;

namespace MyLaboratory.Site.Services
{
    public class MailRepository : IMailRepository
    {
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

        public async Task<string> SendMail(Mail mail)
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