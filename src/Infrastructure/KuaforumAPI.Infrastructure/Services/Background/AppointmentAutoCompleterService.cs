using KuaforumAPI.Domain.Enums;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure.Services.Background
{
    public class AppointmentAutoCompleterService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AppointmentAutoCompleterService> _logger;

        public AppointmentAutoCompleterService(IServiceProvider serviceProvider, ILogger<AppointmentAutoCompleterService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Auto-Completer Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAppointmentsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while auto-completing appointments.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessAppointmentsAsync(CancellationToken stoppingToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dateTimeService = scope.ServiceProvider.GetRequiredService<KuaforumAPI.Application.Interfaces.Services.IDateTimeService>();

                var now = dateTimeService.Now;

                // Find confirmed appointments that have ended and belong to shops with auto-process enabled
                var appointmentsToComplete = await context.Appointments
                    .Include(a => a.Shop)
                    .Where(a => a.Status == AppointmentStatus.Confirmed 
                                && a.EndTime <= now 
                                && a.Shop.IsAutoProcessEnabled)
                    .ToListAsync(stoppingToken);

                if (appointmentsToComplete.Any())
                {
                    foreach (var appointment in appointmentsToComplete)
                    {
                        appointment.Status = AppointmentStatus.Completed;
                        appointment.UpdatedAt = now;
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Auto-completed {appointmentsToComplete.Count} appointments.");
                }
            }
        }
    }
}
