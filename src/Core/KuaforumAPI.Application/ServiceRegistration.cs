using Microsoft.Extensions.DependencyInjection;
using KuaforumAPI.Application.Interfaces.Services;
using KuaforumAPI.Application.Services;
using FluentValidation;

namespace KuaforumAPI.Application
{
    public static class ServiceRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ICoreExampleService, CoreExampleService>();
            services.AddValidatorsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
        }
    }
}
