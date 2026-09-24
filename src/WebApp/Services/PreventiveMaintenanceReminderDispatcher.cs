using ZEGU.Infrastructure.Services;

namespace ZEGU.WebApp.Services
{
    public static class PreventiveMaintenanceReminderDispatcher
    {
        public static void Dispatch(List<PmReminderEvent> events, EmailService emailService, SmsService smsService, WhatsAppService whatsAppService)
        {
            foreach (var evt in events)
            {
                var subject = evt.IsOverdue
                    ? $"OVERDUE: Preventive Maintenance - {evt.Schedule.ScheduleName}"
                    : $"Due Soon: Preventive Maintenance - {evt.Schedule.ScheduleName}";
                var body = evt.IsOverdue
                    ? $"<p>The preventive maintenance schedule <strong>{evt.Schedule.ScheduleName}</strong> was due on {evt.Schedule.NextDue:dd MMM yyyy} and has not been marked performed.</p>"
                    : $"<p>The preventive maintenance schedule <strong>{evt.Schedule.ScheduleName}</strong> is due on {evt.Schedule.NextDue:dd MMM yyyy}.</p>";

                foreach (var recipient in evt.Recipients)
                {
                    if (!string.IsNullOrEmpty(recipient.Email))
                    {
                        _ = emailService.SendEmailAsync(recipient.Email, subject, body, isHtml: true);
                    }
                    if (!string.IsNullOrEmpty(recipient.PhoneNumber))
                    {
                        _ = smsService.SendSmsAsync(recipient.PhoneNumber, subject);
                        _ = whatsAppService.SendWhatsAppAsync(recipient.PhoneNumber, subject);
                    }
                }
            }
        }
    }
}
