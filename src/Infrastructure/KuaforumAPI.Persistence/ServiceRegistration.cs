using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Persistence.Contexts;
using KuaforumAPI.Persistence.Repositories;

namespace KuaforumAPI.Persistence
{
    public static class ServiceRegistration
    {
        public static void AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Medya adresi: DB anahtar saklar, tam URL bu tabandan uretilir.
            // Bos birakilirsa donusturucu devre disi kalir (mevcut tam URL'ler aynen calisir).
            services.Configure<KuaforumAPI.Application.Settings.MediaSettings>(
                configuration.GetSection("Media"));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ICoreExampleRepository, CoreExampleRepository>();
            services.AddScoped<ISalonOwnerApplicationRepository, SalonOwnerApplicationRepository>();
            services.AddScoped<IShopRepository, ShopRepository>();
            services.AddScoped<IShopImageRepository, ShopImageRepository>();

            services.AddScoped<IShopEmployeeRepository, ShopEmployeeRepository>();
            services.AddScoped<ISiteVisitRepository, SiteVisitRepository>();
        }
    }
}
