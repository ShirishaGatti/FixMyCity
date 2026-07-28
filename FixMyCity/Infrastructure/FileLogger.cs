
using System;
using System.IO;
using System.Web.Hosting;

namespace FixMyCity.Infrastructure
{
    public static class FileLogger
    {
        private static readonly string LogPath =
            HostingEnvironment.MapPath("~/App_Data/Logs/errors.log") ?? "errors.log";

        public static void Log(Exception ex, string source)
        {
            try
            {
                string dir = Path.GetDirectoryName(LogPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string entry = string.Format(
                    "[{0}] SOURCE: {1}{2}MSG: {3}{4}STACK: {5}{6}---{7}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    source, Environment.NewLine,
                    ex.Message, Environment.NewLine,
                    ex.StackTrace, Environment.NewLine,
                    Environment.NewLine
                );

                File.AppendAllText(LogPath, entry);
            }
            catch
            {
                // File log also failed — nothing more we can do.
                // User still gets a friendly message from GlobalMvcExceptionFilter.
            }
        }
    }
}