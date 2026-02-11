using KuaforumAPI.Application.DTOs.Service;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IServiceManagementService
    {
        Task CreateCategoryAsync(string ownerId, CreateServiceCategoryDto request);
        Task CreateServiceAsync(string ownerId, CreateShopServiceDto request);
        Task<List<ServiceCategoryDto>> GetShopServicesAsync(string userId);
        Task<List<ServiceCategoryDto>> GetServicesByShopIdAsync(System.Guid shopId);
    }
}
