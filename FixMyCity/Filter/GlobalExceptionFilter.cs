// =============================================
// FILE: ComplaintSystem/Filters/GlobalMvcExceptionFilter.cs
//
// Pure MVC exception filter (IExceptionFilter) — deliberately NOT the
// Web API ExceptionHandler type. Register in App_Start/FilterConfig.cs:
//     filters.Add(new GlobalMvcExceptionFilter());
// =============================================

using FixMyCity.Exceptions;
using System;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace FixMyCity.Filters
{
    public class GlobalMvcExceptionFilter : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled) return;

            var ex = filterContext.Exception;
            string url = filterContext.HttpContext.Request.Url?.ToString() ?? "";
            string user = filterContext.HttpContext.User?.Identity?.Name;

            // ── Where to log ─────────────────────────────────────────────
            if (ex is BusinessException)
            {
                DbLogger.Log(ex, url, user);
            }
            else if (ex is DataAccessException || ex is SqlException)
            {
                FileLogger.Log(ex, url);
            }
            else if (ex is NotFoundException)
            {
                // Normal user behaviour (e.g. viewing a deleted complaint) — no log.
            }
            else
            {
                FileLogger.Log(ex, url);
                DbLogger.Log(ex, url, user);
            }

            // ── Response to the user ────────────────────────────────────
            string friendlyMessage = GetFriendlyMessage(ex);

            if (filterContext.HttpContext.Request.IsAjaxRequest())
            {
                filterContext.Result = new JsonResult
                {
                    Data = new { success = false, message = friendlyMessage },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                filterContext.HttpContext.Response.StatusCode = 200; // keep jQuery's .done() path
            }
            else
            {
                filterContext.Result = new ViewResult { ViewName = "~/Views/Shared/Error.cshtml" };
                filterContext.HttpContext.Response.StatusCode = 500;
            }

            filterContext.ExceptionHandled = true;
        }

        private string GetFriendlyMessage(Exception ex)
        {
            if (ex is NotFoundException) return ex.Message;
            if (ex is BusinessException) return ex.Message;
            if (ex is DataAccessException) return "A database error occurred. Please try again later.";
            return "Something went wrong. Please try again or contact support.";
        }
    }

    // Minimal stand-ins — swap for your real Serilog/file/db sinks.
    public static class FileLogger
    {
        public static void Log(Exception ex, string url)
        {
            try
            {
                string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/Logs/errors.log");
                if (!string.IsNullOrEmpty(logPath))
                {
                    string dir = System.IO.Path.GetDirectoryName(logPath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    System.IO.File.AppendAllText(
                        logPath,
                        $"{DateTime.UtcNow:u} | {url} | {ex}\r\n");
                }
            }
            catch
            {
                // Logging failure should never throw unhandled exceptions
            }
        }
    }

    public static class DbLogger
    {
        public static void Log(Exception ex, string url, string user)
        {
            // Intentionally best-effort: logging must never itself throw
            // and mask the original exception.
            try
            {
                FileLogger.Log(ex, url);
            }
            catch { /* swallow — logging is not allowed to crash the request */ }
        }
    }
}