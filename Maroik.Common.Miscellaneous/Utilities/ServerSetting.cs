using System.Diagnostics;

namespace Maroik.Common.Miscellaneous.Utilities
{
    public static class ServerSetting
    {
        public static string? DomainName { get; set; }
        public static string? SmtpUserName { get; set; }
        public static string? SmtpPassword { get; set; }
        public static string? SmtpHost { get; set; }
        public static int SmtpPort { get; set; }
        public static bool SmtpSSL { get; set; }
        public static string? FromEmail { get; set; }
        public static string? FromFullName { get; set; }
        public static bool IsDefault { get; set; }
        public static int MaxLoginAttempt { get; set; }
        public static int SessionExpireMinutes { get; set; }
        public static int NoticeMaturityDateDay { get; set; }

        public static string ExecuteBashCommand(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true, // 오류 메시지 캡처
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                process.Start();

                string result = process.StandardOutput.ReadToEnd().Trim();
                string error = process.StandardError.ReadToEnd().Trim();

                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error: {error}");  // 콘솔에 오류 출력
                }

                return result;
            }
            catch
            {
                return "";
            }
        }
    }
}