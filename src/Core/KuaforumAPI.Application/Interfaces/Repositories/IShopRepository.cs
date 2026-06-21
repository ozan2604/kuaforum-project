using KuaforumAPI.Application.DTOs.Shop;
using KuaforumAPI.Domain.Entities;
using KuaforumAPI.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IShopRepository : IGenericRepository<Shop>
    {
        Task<Shop> GetByOwnerIdAsync(string ownerId);
        Task<List<Shop>> GetAllByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Shop>> GetAllWithDetailsAsync(string? city = null, string? district = null, string? neighborhood = null);
        Task<(List<Shop> Items, int TotalCount)> GetPagedWithDetailsAsync(string? city, string? district, string? neighborhood, int pageNumber, int pageSize, ShopType? shopType = null);
        Task<List<string>> DeleteShopWithDependenciesAsync(Guid shopId);
        Task UpdateShopCategoriesAsync(Guid shopId, List<int> categoryValues);
        Task UpdateMobileServiceAreasAsync(Guid shopId, List<ServiceAreaDto> areas);
    }
}
