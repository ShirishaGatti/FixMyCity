using System;
using System.Configuration;
using System.Net.Mail;
using System.Configuration;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.IO;
namespace FixMyCity.Service
{
    public interface IMailService
    {
        void SendOtpEmail(string toEmail, string otp);
    }

    public class MailService : IMailService
    {
        /* public void SendOtpEmail(string toEmail, string otp)
         {
             if (string.IsNullOrWhiteSpace(toEmail)) return;

             string smtpEmail = ConfigurationManager.AppSettings["SmtpEmail"];
             string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];

             if (string.IsNullOrWhiteSpace(smtpEmail) || string.IsNullOrWhiteSpace(smtpPassword))
             {
                 return;
             }

             try
             {
                 using (var message = new MailMessage())
                 {
                     message.From = new MailAddress(smtpEmail, "FixMyCity");
                     message.To.Add(new MailAddress(toEmail));
                     message.Subject = "Your FixMyCity Verification Code";
                     message.Body = $@"
                         <div style='font-family: Arial, sans-serif; padding: 20px;'>
                             <h2>FixMyCity Account Verification</h2>
                             <p>Your verification code (OTP) is:</p>
                             <h1 style='color: #0d6efd; letter-spacing: 4px;'>{otp}</h1>
                             <p>This code will expire in 5 minutes. Do not share this code with anyone.</p>
                         </div>";
                     message.IsBodyHtml = true;

                     using (var client = new SmtpClient("smtp.gmail.com", 587))
                     {
                         client.EnableSsl = true;
                         client.UseDefaultCredentials = false;
                         client.Credentials = new System.Net.NetworkCredential(smtpEmail, smtpPassword);
                         client.Send(message);
                     }
                 }
             }
             catch (Exception ex)
             {
                 // Best-effort send: log error using FileLogger
                 FixMyCity.Filters.FileLogger.Log(ex, "EmailService.SendOtpEmail");
             }
         }*/
        /*    public void SendOtpEmail(string toEmail, string otp)
         {
             var fromEmail = ConfigurationManager.AppSettings["SmtpEmail"];
             var fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];

             var message = new MailMessage();

             message.From = new MailAddress(fromEmail, "Food Ordering System");
             message.To.Add(toEmail);
             message.Subject = "Your One-Time Password (OTP)";
             message.IsBodyHtml = true;
             string templatePath = HttpContext.Current.Server.MapPath("~/Views/Account/LoginOtp.html");

             string body = File.ReadAllText(templatePath);

             body = body.Replace("{{OTP}}", otp);

             message.IsBodyHtml = true;
             message.Body = body;


             using (var smtp = new SmtpClient("smtp.gmail.com", 587))
             {
                 smtp.Credentials = new NetworkCredential(fromEmail, fromPassword);
                 smtp.EnableSsl = true;
                 smtp.Send(message);
             }
         }*/
        public void SendOtpEmail(string toEmail, string otp)
        {
            var fromEmail = ConfigurationManager.AppSettings["SmtpEmail"];
            var fromPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            if (string.IsNullOrWhiteSpace(smtpEmail) || string.IsNullOrWhiteSpace(smtpPassword))
            {
                return;
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("FixMyCity", fromEmail));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = "Your One-Time Password (OTP)";

                string templatePath = HttpContext.Current.Server.MapPath("~/Views/Account/LoginOtp.html");
                string body = File.ReadAllText(templatePath).Replace("{{OTP}}", otp);

                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(fromEmail, fromPassword);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                // Best-effort send: log error using FileLogger
                //string msg = "Invalid email or password. Please try again.";
                throw new DataAccessException("Failed to send OTP email", "Otp_Verification", ex);

               // FixMyCity.Filters.FileLogger.Log(ex, "EmailService.SendOtpEmail");
            }
        }
    }
}
