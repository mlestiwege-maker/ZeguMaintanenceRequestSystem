using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ZEGU.WebApp.Services
{
    public class WhatsAppService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppService> _logger;

        public WhatsAppService(IConfiguration configuration, ILogger<WhatsAppService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendWhatsAppAsync(string phoneNumber, string message)
        {
            try
            {
                var twilioSettings = _configuration.GetSection("TwilioSettings");
                var accountSid = twilioSettings["AccountSid"] ?? string.Empty;
                var authToken = twilioSettings["AuthToken"] ?? string.Empty;
                var fromPhone = twilioSettings["WhatsAppFromPhone"] ?? string.Empty;

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromPhone))
                {
                    _logger.LogWarning("Twilio WhatsApp settings not configured. WhatsApp message not sent to {Phone}", phoneNumber);
                    return;
                }

                TwilioClient.Init(accountSid, authToken);

                var messageResource = await MessageResource.CreateAsync(
                    to: new PhoneNumber($"whatsapp:{phoneNumber}"),
                    from: new PhoneNumber($"whatsapp:{fromPhone}"),
                    body: message
                );

                _logger.LogInformation("WhatsApp message sent successfully to {Phone}. SID: {Sid}", phoneNumber, messageResource.Sid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp message to {Phone}", phoneNumber);
                throw;
            }
        }

        public async Task SendMaintenanceNotificationAsync(string phoneNumber, string userName, string requestNumber, string status)
        {
            var message = $"*ZEGU University Maintenance System*\n\nDear {userName},\n\nYour maintenance request *{requestNumber}* status has been updated to: *{status}*\n\nPlease log in to the system for more details.";
            await SendWhatsAppAsync(phoneNumber, message);
        }
    }
}
