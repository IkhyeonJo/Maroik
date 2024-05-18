using Maroik.Common.DataAccess.Models;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Maroik.WebSite.Extensions
{
    public static class DateTimeExtension
    {
        public static DateTime ConvertTimeByTimeZoneIanaId(this DateTime value, string timeZoneIanaId)
        {
            try
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(value, timeZoneIanaId);
            }
            catch
            {
                return value;
            }
        }
    }
}