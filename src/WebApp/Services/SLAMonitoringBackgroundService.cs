using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZEGU.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ZEGU.WebApp.Services
{
    public class SLAMonitoringBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SLAMonitoringBackgroundService> _logger;

        public SLAMonitoringBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SLAMonitoringBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SLA Monitoring Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var slaService = scope.ServiceProvider.GetRequiredService<SLAMonitoringService>();
                    await slaService.CheckOverdueRequestsAsync();
                    _logger.LogInformation("SLA check completed at: {time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking SLA breaches.");
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
