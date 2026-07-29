using FixMyCity.Infrastructure;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace FixMyCity.Service
{
    public interface IMailService
    {
        void SendOtpEmail(string toEmail, string otp);
    }

    /// <summary>
    /// Best-effort SMTP sender using System.Net.Mail (BCL — no extra NuGet package required).
    /// MailKit would be preferable for modern TLS support, but it cannot be installed
    /// in this project environment, so we use SmtpClient with EnableSsl = true instead.
    ///
    /// The send is intentionally best-effort: a mail failure is logged but never
    /// re-thrown, so a transient SMTP outage cannot block the login flow.
    /// Configure in Web.config appSettings:
    ///   <add key="SmtpEmail"    value="your@gmail.com" />
    ///   <add key="SmtpPassword" value="your-app-password" />
    /// </summary>
    public class MailService : IMailService
    {
        public void SendOtpEmail(string toEmail, string otp)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            string fromEmail    = ConfigurationManager.AppSettings["SmtpEmail"];
            string fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            // If credentials are missing we skip silently — allows development
            // without a real SMTP account configured.
            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                return;

            try
            {
                string body = BuildHtmlBody(otp);

                using (var message = new MailMessage())
                {
                    message.From       = new MailAddress(fromEmail, "FixMyCity");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject    = "Your FixMyCity Verification Code";
                    message.Body       = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl          = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials        = new NetworkCredential(fromEmail, fromPassword);
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                // Best-effort: log the failure but do NOT re-throw.
                // A mail outage must never prevent a user from logging in.
                FileLogger.Log(ex, "MailService.SendOtpEmail");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to load ~/Views/Account/LoginOtp.html and substitute {{OTP}}.
        /// Falls back to a simple inline template if the file is missing,
        /// so the service never throws a FileNotFoundException in production.
        /// </summary>
        private static string BuildHtmlBody(string otp)
        {
            try
            {
                string templatePath = HttpContext.Current?.Server.MapPath("~/Views/Account/LoginOtp.html");
                if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
                    return File.ReadAllText(templatePath).Replace("{{OTP}}", otp);
            }
            catch
            {
                // Template load failed — fall through to inline body.
            }

            return string.Format(@"
<div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 32px;
            border: 1px solid #e0e0e0; border-radius: 8px;'>
  <h2 style='color: #1a73e8;'>FixMyCity</h2>
  <p>Your one-time verification code is:</p>
  <h1 style='letter-spacing: 8px; color: #202124;'>{0}</h1>
  <p style='color: #5f6368; font-size: 13px;'>
    This code expires in 5 minutes. Do not share it with anyone.
  </p>
</div>", otp);
        }
    }
}
