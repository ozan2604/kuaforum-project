using KuaforumAPI.Application.DTOs.Block;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IShopBlockService
    {
        Task BlockCustomerAsync(string? staffUserId, Guid shopId, BlockCustomerDto dto);
        Task UnblockCustomerAsync(string? ownerId, Guid shopId, string customerId);
        Task<List<BlockedCustomerDto>> GetBlockedCustomersAsync(string? ownerId, Guid shopId);
        Task<CustomerShopInfoDto?> GetCustomerByPhoneAsync(string? ownerId, Guid shopId, string phone);
    }
}
