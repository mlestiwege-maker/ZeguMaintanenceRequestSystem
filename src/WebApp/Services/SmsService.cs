using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ZEGU.WebApp.Services
{
    public class SmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var twilioSettings = _configuration.GetSection("TwilioSettings");
                var accountSid = twilioSettings["AccountSid"] ?? string.Empty;
                var authToken = twilioSettings["AuthToken"] ?? string.Empty;
                var fromPhone = twilioSettings["FromPhone"] ?? string.Empty;

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
                {
                    _logger.LogWarning("Twilio settings not configured. SMS not sent to {Phone}", phoneNumber);
                    return;
                }

                TwilioClient.Init(accountSid, authToken);

                var messageResource = await MessageResource.CreateAsync(
                    to: new PhoneNumber(phoneNumber),
                    from: new PhoneNumber(fromPhone),
                    body: message
                );

                _logger.LogInformation("SMS sent successfully to {Phone}. SID: {Sid}", phoneNumber, messageResource.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {Phone}", phoneNumber);
                throw;
            }
        }

        public async Task SendMaintenanceNotificationAsync(string phoneNumber, string userName, string requestNumber, string status)
        {
            var message = $"Dear {userName}, your maintenance request {requestNumber} status has been updated to: {status}. Please log in to ZEGU Maintenance System for details.";
            await SendSmsAsync(phoneNumber, message);
        }
    }
}
