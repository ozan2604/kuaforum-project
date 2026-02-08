using KuaforumAPI.Domain.Entities;
using System.Threading.Tasks;

namespace KuaforumAPI.Application.Interfaces.Repositories
{
    public interface IShopRepository : IGenericRepository<Shop>
    {
        Task<Shop> GetByOwnerIdAsync(string ownerId);
    }
}
