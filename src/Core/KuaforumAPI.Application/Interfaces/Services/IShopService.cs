using KuaforumAPI.Application.DTOs.Shop;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Services
{
    public interface IShopService
    {
        Task CreateShopAsync(string userId, CreateShopDto request);
        Task<ShopDto> GetShopByOwnerIdAsync(string userId);
        Task UpdateShopAsync(string userId, CreateShopDto request);
    }
}
