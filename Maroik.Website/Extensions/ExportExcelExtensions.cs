using Maroik.Core.Contract.Extensions;

namespace Maroik.Website.Extensions;

/// <summary>
/// Cross-cutting helpers for Excel export operations.
/// </summary>
public static class ExportExcelExtensions
{
    extension(string prefix)
    {
        /// <summary>Builds a timestamped Excel filename from a user-supplied prefix.</summary>
        public string ToExcelFileName(string timeZoneIanaId)
            => $"{prefix}-{DateTime.UtcNow.ConvertTimeByTimeZoneIanaId(timeZoneIanaId):yyyy-MM-dd-HH-mm-ss-fff}.xlsx";
    }
}
