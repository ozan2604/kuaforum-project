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
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ICoreExampleRepository, CoreExampleRepository>();
            services.AddScoped<ISalonOwnerApplicationRepository, SalonOwnerApplicationRepository>();
            services.AddScoped<IShopRepository, ShopRepository>();
            services.AddScoped<IShopImageRepository, ShopImageRepository>();
            services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        }
    }
}
