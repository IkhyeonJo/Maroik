namespace Maroik.WebSite.Models.ViewModels.Calendar
{
    public class CalendarEventOutputViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool AllDay { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartDateTimeZoneIanaId { get; set; }
        public string EndDateTimeZoneIanaId { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public IFormFile CalendarEventUploadedFile { get; set; }
        public int CalendarId { get; set; }
        public string Status { get; set; }
        public string SerializedCalendarReminders { get; set; }
        public string HtmlColorCode { get; set; }
        public string DisplayStartDate { get; set; }
        public string DisplayEndDate { get; set; }
        public string DisplayStartDateTimeZone { get; set; }
        public string DisplayEndDateTimeZone { get; set; }
    }
}