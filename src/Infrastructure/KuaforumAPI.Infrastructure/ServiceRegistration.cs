using FluentValidation;
using KuaforumAPI.Application.DTOs.Employee;
using KuaforumAPI.Application.DTOs.Service;
using KuaforumAPI.Application.Interfaces.Repositories;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Settings;
using KuaforumAPI.Application.Validators;
using KuaforumAPI.Infrastructure.Services;
using KuaforumAPI.Persistence.Contexts;
using KuaforumAPI.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KuaforumAPI.Infrastructure
{
    public static class ServiceRegistration
    {
        public static void AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // NetGSM SMS servisi
            services.Configure<NetGsmSettings>(configuration.GetSection("NetGsm"));
            services.AddHttpClient("netgsm", client =>
            {
                client.BaseAddress = new Uri("https://api.netgsm.com.tr/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddScoped<ISmsService, NetGsmSmsService>();

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISalonApplicationService, SalonApplicationService>();
            services.AddScoped<IShopService, ShopService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IServiceManagementService, ServiceManagementService>();
            services.AddScoped<IAppointmentService, AppointmentService>();
            services.AddScoped<IFavoriteService, FavoriteService>();
            services.AddScoped<IReviewService, ReviewService>();
            // Timezone & Localization
            services.AddScoped<IDateTimeService, DateTimeService>();

            services.AddScoped<IValidator<CreateEmployeeDto>, CreateEmployeeValidator>();
            services.AddScoped<IValidator<CreateServiceCategoryDto>, CreateServiceCategoryValidator>();
            services.AddScoped<IValidator<CreateShopServiceDto>, CreateShopServiceValidator>();

            // Background Services
            services.AddHostedService<Services.Background.AppointmentAutoCompleterService>();
        }
    }
}
