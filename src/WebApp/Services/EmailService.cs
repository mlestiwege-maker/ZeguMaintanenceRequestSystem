using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ZEGU.WebApp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var host = smtpSettings["Host"] ?? "smtp.gmail.com";
                var port = int.Parse(smtpSettings["Port"] ?? "587");
                var username = smtpSettings["Username"] ?? string.Empty;
                var password = smtpSettings["Password"] ?? string.Empty;
                var fromEmail = smtpSettings["FromEmail"] ?? username;
                var fromName = smtpSettings["FromName"] ?? "ZEGU Maintenance";
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("SMTP settings not configured. Email not sent to {Email}", toEmail);
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(fromName, fromEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder();
                if (isHtml)
                {
                    builder.HtmlBody = body;
                }
                else
                {
                    builder.TextBody = body;
                }
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(host, port, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
                await smtp.AuthenticateAsync(username, password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendMaintenanceNotificationAsync(string toEmail, string userName, string requestNumber, string status, string? notes = null)
        {
            var subject = $"Maintenance Request {requestNumber} - Status Update";
            var body = $@"<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #0d6efd; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f8f9fa; }}
        .footer {{ padding: 20px; text-align: center; font-size: 12px; color: #666; }}
        .status {{ display: inline-block; padding: 5px 15px; border-radius: 20px; font-weight: bold; margin: 10px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>ZEGU University Maintenance System</h2>
        </div>
        <div class='content'>
            <p>Dear {userName},</p>
            <p>Your maintenance request <strong>{requestNumber}</strong> status has been updated.</p>
            <p>New Status: <span class='status'>{status}</span></p>
            {(!string.IsNullOrEmpty(notes) ? $"<p><strong>Notes:</strong> {notes}</p>" : "")}
            <p>Please log in to the system to view more details.</p>
        </div>
        <div class='footer'>
            <p>© {DateTime.Now.Year} ZEGU University Maintenance Request System</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(toEmail, subject, body, true);
        }
    }
}
