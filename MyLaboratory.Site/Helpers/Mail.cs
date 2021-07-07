using System.Collections.Generic;

namespace MyLaboratory.Site.Helpers
{
    public class Mail
    {
        public string FromMailId { get; set; } = "***REMOVED***";
        public string FromMailIdPassword { get; set; } = "***REMOVED***";
        public List<string> ToMailIds { get; set; } = new List<string>();
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public bool IsBodyHTML { get; set; } = true;
        public List<string> Attachments { get; set; } = new List<string>();
    }
}