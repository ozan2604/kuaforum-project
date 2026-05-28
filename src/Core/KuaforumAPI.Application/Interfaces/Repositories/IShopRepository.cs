using KuaforumAPI.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IShopRepository : IGenericRepository<Shop>
    {
        Task<Shop> GetByOwnerIdAsync(string ownerId);
        Task<List<Shop>> GetAllByOwnerIdAsync(string ownerId);
        Task<IEnumerable<Shop>> GetAllWithDetailsAsync(string? city = null, string? district = null, string? neighborhood = null);
        Task<(List<Shop> Items, int TotalCount)> GetPagedWithDetailsAsync(string? city, string? district, string? neighborhood, int pageNumber, int pageSize);
        Task<List<string>> DeleteShopWithDependenciesAsync(Guid shopId);
        Task UpdateShopCategoriesAsync(Guid shopId, List<int> categoryValues);
    }
}
