using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KuaforumAPI.Infrastructure.Services.Background
{
    // Uygulama başladığında Code'u olmayan salonlara bir kez kod üretir.
    public class ShopCodeSeederService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ShopCodeSeederService> _logger;

        public ShopCodeSeederService(IServiceProvider serviceProvider, ILogger<ShopCodeSeederService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var codeGenerator = scope.ServiceProvider.GetRequiredService<IShopCodeGeneratorService>();

            var shopsWithoutCode = await context.Shops
                .Where(s => s.Code == null || s.Code == "")
                .ToListAsync(cancellationToken);

            if (shopsWithoutCode.Count == 0) return;

            _logger.LogInformation("Kodsuz {Count} salon için kod üretiliyor.", shopsWithoutCode.Count);

            foreach (var shop in shopsWithoutCode)
            {
                try
                {
                    shop.Code = await codeGenerator.GenerateAsync(shop.City ?? "");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Salon {ShopId} için kod üretilemedi.", shop.Id);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Salon kodu seeding tamamlandı.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
