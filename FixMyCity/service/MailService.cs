using FixMyCity.Infrastructure;
using System;
using System.Collections.Generic;
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
        void SendAssignmentOtpEmail(string toEmail, string officerName, string complaintNumber, string complaintTitle, string priorityName, string otp);
        void SendRoleChangedEmail(string toEmail, string citizenName, string roleName, string deptName);
        void SendComplaintProgressEmail(string toEmail, string citizenName, string complaintNumber, string complaintTitle, string oldStatus, string newStatus);
        void SendComplaintAssignedEmail(string toEmail, string citizenName, string complaintNumber, string complaintTitle, string officerName);
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
                string body = BuildEmailBody(
                    "~/Views/Account/LoginOtp.html",
                    new Dictionary<string, string> { { "{{OTP}}", otp } },
                    string.Format("<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'><h2 style='color:#0d6efd;margin-top:0;'>FixMyCity</h2><p>Your one-time verification code is:</p><h1 style='letter-spacing:8px;color:#202124;'>{0}</h1><p style='color:#5f6368;font-size:13px;'>This code expires in 5 minutes. Do not share it with anyone.</p></div>", otp));

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
        /// Sends an OTP to an officer who has just been assigned a complaint.
        /// Uses a dedicated mail body (distinct from the login OTP) so the
        /// recipient immediately understands this code is for the assignment.
        /// </summary>
        public void SendAssignmentOtpEmail(string toEmail, string officerName, string complaintNumber, string complaintTitle, string priorityName, string otp)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            string fromEmail    = ConfigurationManager.AppSettings["SmtpEmail"];
            string fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                return;

            try
            {
                string priorityColor = PriorityColor(priorityName);
                string priorityLabel = string.IsNullOrWhiteSpace(priorityName) ? "Not set" : priorityName;

                string body = BuildEmailBody(
                    "~/Views/Email/AssignmentOtp.html",
                    new Dictionary<string, string>
                    {
                        { "{{OFFICER_NAME}}", officerName },
                        { "{{COMPLAINT_NUMBER}}", complaintNumber },
                        { "{{COMPLAINT_TITLE}}", complaintTitle },
                        { "{{PRIORITY_NAME}}", priorityLabel },
                        { "{{PRIORITY_COLOR}}", priorityColor },
                        { "{{OTP}}", otp }
                    },
                    string.Format("<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'><h2 style='color:#0d6efd;margin-top:0;'>FixMyCity</h2><p>Hello {0}, a complaint has been assigned to you.</p><p><b>Complaint:</b> {1}<br><b>Title:</b> {2}<br><b>Priority:</b> <span style='background:{3};color:#fff;padding:2px 10px;border-radius:12px;'>{4}</span></p><p>Use this OTP to sign in and take ownership:</p><h1 style='letter-spacing:8px;color:#202124;'>{5}</h1><p style='color:#5f6368;font-size:13px;'>This code expires in 5 minutes. Do not share it with anyone.</p></div>", officerName, complaintNumber, complaintTitle, priorityColor, priorityLabel, otp));

                using (var message = new MailMessage())
                {
                    message.From       = new MailAddress(fromEmail, "FixMyCity");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject    = $"New Complaint Assigned {complaintNumber}";
                    message.Body       = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl             = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials           = new NetworkCredential(fromEmail, fromPassword);
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log(ex, "MailService.SendAssignmentOtpEmail");
            }
        }

        /// <summary>
        /// Sends a notification to a citizen whose role was just changed to Officer,
        /// including the department they were assigned to.
        /// </summary>
        public void SendRoleChangedEmail(string toEmail, string citizenName, string roleName, string deptName)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            string fromEmail    = ConfigurationManager.AppSettings["SmtpEmail"];
            string fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                return;

            try
            {
                string deptLine = string.IsNullOrWhiteSpace(deptName)
                    ? "<p style='margin:0;color:#6b7280;font-size:14px;line-height:1.6;'>No department has been assigned yet.</p>"
                    : string.Format("<p style='margin:6px 0 0;color:#4b5563;font-size:15px;line-height:1.7;'><strong>Department:</strong> {0}</p>", deptName);

                string body = BuildEmailBody(
                    "~/Views/Email/RoleChanged.html",
                    new Dictionary<string, string>
                    {
                        { "{{CITIZEN_NAME}}", citizenName },
                        { "{{ROLE_NAME}}", roleName },
                        { "{{DEPT_LINE}}", deptLine }
                    },
                    string.Format("<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'><h2 style='color:#0d6efd;margin-top:0;'>FixMyCity</h2><p>Hello {0}, your account role has been updated to <b>{1}</b>.</p>{2}</div>", citizenName, roleName, deptLine));

                using (var message = new MailMessage())
                {
                    message.From       = new MailAddress(fromEmail, "FixMyCity");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject    = "Your FixMyCity Role Has Been Updated";
                    message.Body       = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl             = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials           = new NetworkCredential(fromEmail, fromPassword);
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log(ex, "MailService.SendRoleChangedEmail");
            }
        }

        /// <summary>
        /// Sends a notification to the citizen who raised a complaint when its
        /// progress (status) changes.
        /// </summary>
        public void SendComplaintProgressEmail(string toEmail, string citizenName, string complaintNumber, string complaintTitle, string oldStatus, string newStatus)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            string fromEmail    = ConfigurationManager.AppSettings["SmtpEmail"];
            string fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                return;

            try
            {
                string body = BuildEmailBody(
                    "~/Views/Email/ComplaintProgress.html",
                    new Dictionary<string, string>
                    {
                        { "{{CITIZEN_NAME}}", citizenName },
                        { "{{COMPLAINT_NUMBER}}", complaintNumber },
                        { "{{COMPLAINT_TITLE}}", complaintTitle },
                        { "{{OLD_STATUS}}", oldStatus },
                        { "{{NEW_STATUS}}", newStatus }
                    },
                    string.Format("<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'><h2 style='color:#0d6efd;margin-top:0;'>FixMyCity</h2><p>Hello {0}, the progress of your complaint has been updated.</p><p><b>Complaint:</b> {1}<br><b>Title:</b> {2}<br><b>Status:</b> {3} &#8594; <b>{4}</b></p></div>", citizenName, complaintNumber, complaintTitle, oldStatus, newStatus));

                using (var message = new MailMessage())
                {
                    message.From       = new MailAddress(fromEmail, "FixMyCity");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject    = $"Progress Update {complaintNumber}";
                    message.Body       = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl             = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials           = new NetworkCredential(fromEmail, fromPassword);
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log(ex, "MailService.SendComplaintProgressEmail");
            }
        }

        /// <summary>
        /// Sends a notification to the citizen who raised a complaint when the
        /// complaint gets assigned to (or re-assigned to) an officer.
        /// </summary>
        public void SendComplaintAssignedEmail(string toEmail, string citizenName, string complaintNumber, string complaintTitle, string officerName)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            string fromEmail    = ConfigurationManager.AppSettings["SmtpEmail"];
            string fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

            if (string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(fromPassword))
                return;

            try
            {
                string body = BuildEmailBody(
                    "~/Views/Email/ComplaintAssigned.html",
                    new Dictionary<string, string>
                    {
                        { "{{CITIZEN_NAME}}", citizenName },
                        { "{{COMPLAINT_NUMBER}}", complaintNumber },
                        { "{{COMPLAINT_TITLE}}", complaintTitle },
                        { "{{OFFICER_NAME}}", officerName }
                    },
                    string.Format("<div style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;border:1px solid #e0e0e0;border-radius:8px;'><h2 style='color:#0d6efd;margin-top:0;'>FixMyCity</h2><p>Hello {0}, your complaint has been assigned to an officer and is now being handled.</p><p><b>Complaint:</b> {1}<br><b>Title:</b> {2}<br><b>Assigned Officer:</b> {3}</p></div>", citizenName, complaintNumber, complaintTitle, officerName));

                using (var message = new MailMessage())
                {
                    message.From       = new MailAddress(fromEmail, "FixMyCity");
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject    = $"Complaint {complaintNumber} Has Been Assigned";
                    message.Body       = body;
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl             = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials           = new NetworkCredential(fromEmail, fromPassword);
                        smtp.Send(message);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.Log(ex, "MailService.SendComplaintAssignedEmail");
            }
        }

        private static string PriorityColor(string priorityName)
        {
            switch ((priorityName ?? "").Trim().ToLowerInvariant())
            {
                case "high":   return "#dc2626"; // red
                case "medium": return "#ca8a04"; // yellow
                case "low":    return "#16a34a"; // green
                default:       return "#6b7280"; // gray
            }
        }

        /// <summary>
        /// Loads an HTML email template from disk and substitutes the given
        /// placeholders (e.g. {{OTP}}). Falls back to the supplied inline body
        /// if the file is missing, so the service never throws a
        /// FileNotFoundException in production.
        /// </summary>
        private static string BuildEmailBody(string templateVirtualPath, Dictionary<string, string> tokens, string fallback)
        {
            try
            {
                string templatePath = HttpContext.Current?.Server.MapPath(templateVirtualPath);
                if (!string.IsNullOrEmpty(templatePath) && File.Exists(templatePath))
                {
                    string html = File.ReadAllText(templatePath);
                    if (tokens != null)
                        foreach (var token in tokens)
                            html = html.Replace(token.Key, token.Value);
                    return html;
                }
            }
            catch
            {
                // Template load failed — fall through to inline fallback.
            }

            return fallback;
        }
    }
}
