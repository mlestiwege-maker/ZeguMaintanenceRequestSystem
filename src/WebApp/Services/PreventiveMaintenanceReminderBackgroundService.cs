using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZEGU.Infrastructure.Services;

namespace ZEGU.WebApp.Services
{
    public class PreventiveMaintenanceReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PreventiveMaintenanceReminderBackgroundService> _logger;

        public PreventiveMaintenanceReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PreventiveMaintenanceReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Preventive Maintenance Reminder Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var reminderService = scope.ServiceProvider.GetRequiredService<PreventiveMaintenanceReminderService>();
                    var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();
                    var smsService = scope.ServiceProvider.GetRequiredService<SmsService>();
                    var whatsAppService = scope.ServiceProvider.GetRequiredService<WhatsAppService>();

                    var events = await reminderService.CheckDueSchedulesAsync();
                    PreventiveMaintenanceReminderDispatcher.Dispatch(events, emailService, smsService, whatsAppService);

                    _logger.LogInformation("Preventive maintenance reminder check completed at: {time}, {count} reminder(s) sent.", DateTime.UtcNow, events.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking preventive maintenance schedules.");
                }

                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
