using System.Net;
using System.Net.Mail;

namespace TruckManagement.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SmtpEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // Retrieve SMTP settings from config or appsettings
            var host = _config["Smtp:Host"];
            var port = int.Parse(_config["Smtp:Port"]);
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var fromAddress = _config["Smtp:FromAddress"];

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true // If you want HTML emails
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
