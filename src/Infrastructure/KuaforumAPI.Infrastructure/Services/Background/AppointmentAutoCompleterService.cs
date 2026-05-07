using KuaforumAPI.Application.Constants;
using KuaforumAPI.Application.Interfaces.Services;
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
                var dateTimeService = scope.ServiceProvider.GetRequiredService<IDateTimeService>();
                var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();

                var now = dateTimeService.Now;
                _logger.LogDebug("Auto-process running at {Now} (Turkey time).", now);

                // 1. Bekleyen (Pending) randevuları otomatik onayla (IsAutoProcessEnabled olan salonlar için, henüz süresi dolmamış)
                var appointmentsToApprove = await context.Appointments
                    .Include(a => a.Shop)
                    .Include(a => a.User)
                    .Where(a => a.Status == AppointmentStatus.Pending
                                && a.StartTime > now
                                && a.Shop.IsAutoProcessEnabled)
                    .ToListAsync(stoppingToken);

                foreach (var appointment in appointmentsToApprove)
                {
                    appointment.Status = AppointmentStatus.Confirmed;
                    appointment.UpdatedAt = now;
                }

                if (appointmentsToApprove.Count > 0)
                    _logger.LogInformation("Auto-approved {Count} pending appointments.", appointmentsToApprove.Count);

                // 2. Onaylanmış randevuları otomatik tamamla (IsAutoProcessEnabled olan salonlar için, süresi dolmuş)
                var appointmentsToComplete = await context.Appointments
                    .Include(a => a.Shop)
                    .Include(a => a.User)
                    .Include(a => a.ShopService)
                    .Where(a => a.Status == AppointmentStatus.Confirmed
                                && a.EndTime <= now
                                && a.Shop.IsAutoProcessEnabled)
                    .ToListAsync(stoppingToken);

                foreach (var appointment in appointmentsToComplete)
                {
                    appointment.Status = AppointmentStatus.Completed;
                    appointment.UpdatedAt = now;
                }

                if (appointmentsToComplete.Count > 0)
                    _logger.LogInformation("Auto-completed {Count} appointments.", appointmentsToComplete.Count);

                // 3. Saati geçmiş Pending randevuları otomatik reddet (onaylanmamış kalmış)
                var appointmentsToExpire = await context.Appointments
                    .Where(a => a.Status == AppointmentStatus.Pending && a.EndTime <= now)
                    .ToListAsync(stoppingToken);

                foreach (var appointment in appointmentsToExpire)
                {
                    appointment.Status = AppointmentStatus.Rejected;
                    appointment.UpdatedAt = now;
                }

                if (appointmentsToExpire.Count > 0)
                    _logger.LogInformation("Auto-rejected {Count} expired pending appointments.", appointmentsToExpire.Count);

                if (appointmentsToApprove.Count > 0 || appointmentsToComplete.Count > 0 || appointmentsToExpire.Count > 0)
                    await context.SaveChangesAsync(stoppingToken);

                // SMS bildirimleri (SaveChanges sonrası, hata olursa ana akışı etkilemez)
                foreach (var appointment in appointmentsToApprove)
                {
                    try
                    {
                        if (appointment.User?.PhoneNumber != null)
                            await smsService.SendSmsAsync(
                                appointment.User.PhoneNumber,
                                SmsTemplates.AppointmentAutoConfirmed(appointment.Shop.Name, appointment.StartTime));
                    }
                    catch { }
                }

                foreach (var appointment in appointmentsToComplete)
                {
                    try
                    {
                        if (appointment.User?.PhoneNumber != null)
                            await smsService.SendSmsAsync(
                                appointment.User.PhoneNumber,
                                SmsTemplates.AppointmentCompleted(appointment.Shop.Name, appointment.ShopService?.Name ?? ""));
                    }
                    catch { }
                }
            }
        }
    }
}
