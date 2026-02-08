using KuaforumAPI.Application.Constants;
using KuaforumAPI.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace KuaforumAPI.Persistence.Seeds
{
    public static class AdminUserSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminUser = new ApplicationUser
            {
                FirstName = "System",
                LastName = "Admin",
                UserName = "admin@kuaforum.com",
                Email = "admin@kuaforum.com",
                EmailConfirmed = true
            };

            var user = await userManager.FindByEmailAsync(adminUser.Email);
            if (user == null)
            {
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }
        }
    }
}
