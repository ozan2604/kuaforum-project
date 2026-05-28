using KuaforumAPI.Application.DTOs.Service;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IServiceManagementService
    {
        Task CreateCategoryAsync(Guid shopId, string ownerId, CreateServiceCategoryDto request);
        Task CreateServiceAsync(Guid shopId, string ownerId, CreateShopServiceDto request);
        Task<List<ServiceCategoryDto>> GetShopServicesAsync(Guid shopId, string ownerId);
        Task<List<ServiceCategoryDto>> GetServicesByShopIdAsync(System.Guid shopId);
        Task UpdateCategoryAsync(Guid shopId, string ownerId, System.Guid categoryId, UpdateServiceCategoryDto request);
        Task DeleteCategoryAsync(Guid shopId, string ownerId, System.Guid categoryId);
        Task UpdateServiceAsync(Guid shopId, string ownerId, System.Guid serviceId, UpdateShopServiceDto request);
        Task DeleteServiceAsync(Guid shopId, string ownerId, System.Guid serviceId);
    }
}
