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
        public string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel)
        {
            string url = $"{ServerSetting.DomainName}Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(loginInputViewModel.RegistrationToken)}";
            return @$"<div style='text-align:centre;'>
                        <h1>Welcome to MyLaboratory Site<h1>
                        <h3>Click the link below to verify your Email<h3>
                        <h3>If this link does not work, please copy & paste this link to your Internet URL<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
        }

        public string GetMailResetPasswordBody(LoginInputViewModel loginInputViewModel)
        {
            string url = $"{ServerSetting.DomainName}Account/ResetPassword?token={Uri.EscapeDataString(loginInputViewModel.ResetPasswordToken)}";
            return @$"<div style='text-align:centre;'>
                        <h1>Welcome to MyLaboratory Site<h1>
                        <h3>Click the link below to reset your password<h3>
                        <h3>If this link does not work, please copy & paste this link to your Internet URL<h3>
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