using KuaforumAPI.Application.DTOs.Service;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IServiceManagementService
    {
        Task CreateCategoryAsync(string ownerId, CreateServiceCategoryDto request);
        Task CreateServiceAsync(string ownerId, CreateShopServiceDto request);
        Task<List<ServiceCategoryDto>> GetShopServicesAsync(string userId); // For Owner
        // Task<List<ServiceCategoryDto>> GetShopServicesPublicAsync(Guid shopId); // For Customers (Later)
    }
}
