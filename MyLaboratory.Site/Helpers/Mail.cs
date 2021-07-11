using MyLaboratory.Site.Common;
using System.Collections.Generic;

namespace MyLaboratory.Site.Helpers
{
    public class Mail
    {
        public Mail()
        {
            this.FromMailId = Global.FromMailId;
            this.FromMailIdPassword = Global.FromMailIdPassword;
        }
        public string FromMailId { get; set; }
        public string FromMailIdPassword { get; set; }
        public List<string> ToMailIds { get; set; } = new List<string>();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsBodyHTML { get; set; } = true;
        public List<string> Attachments { get; set; } = new List<string>();
    }
}