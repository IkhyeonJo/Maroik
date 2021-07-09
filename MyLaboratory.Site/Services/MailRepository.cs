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
        public string GetMailBody(LoginInputViewModel loginInputViewModel)
        {
            string url = $"{Global.DomainName}api/LoginInfos/ConfirmMail/{loginInputViewModel.Guid}";
            return @$"<div style='text-align:centre;'>
                                        <h1>Welcome to MyLaboratory Site<h1>
                                        <h3>Click the button below to verify your Email<h3>
                                        <form method='get' action='{url}' style='display : inline;'>
                                            <button type='submit' style='display : block;
                                                                        text-align : centre;
                                                                        font-weight : bold;
                                                                        background-color : #008CBA;
                                                                        font-size : 16px;
                                                                        border-radius : 10px;
                                                                        color : #ffffff;
                                                                        cursor : pointer
                                                                        width : 100%;
                                                                        padding : 10px;'>
                                            Confirm Mail
                                            </button>
                                        </form>
                                    </div>";
        }

        public async Task<string> SendMail(Mail mail)
        {
            try
            {
                using (MailMessage mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(mail.FromMailId);
                    mail.ToMailIds.ForEach(x => {
                        mailMessage.To.Add(x);
                    });
                    mailMessage.Subject = mail.Subject;
                    mailMessage.Body = mail.Body;
                    mailMessage.IsBodyHtml = mail.IsBodyHTML;
                    mail.Attachments.ForEach(x => {
                        mailMessage.Attachments.Add(new Attachment(x));
                    });

                    using (SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(mail.FromMailId, mail.FromMailIdPassword);
                        smtp.EnableSsl = true;
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