using System.Runtime.InteropServices;

namespace Maroik.WebSite.Common
{
    public static class OSTimeZone
    {
        private static string destinationTimeZoneId;
        public static string DestinationTimeZoneId
        {
            get
            {
                destinationTimeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Korea Standard Time" : "Asia/Seoul";

                return destinationTimeZoneId;
            }
        }
    }
}